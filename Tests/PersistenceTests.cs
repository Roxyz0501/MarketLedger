using System.Text.Json;
using MarketLedger;

internal static class PersistenceTests
{
    public static void Run(string directory)
    {
        Directory.CreateDirectory(directory); int checks=0;
        void Check(bool condition,string label) { checks++; if(!condition) throw new Exception(label); }
        void Wait(Func<bool> condition,string label) { Check(SpinWait.SpinUntil(condition,5000),label); }
        var now=DateTimeOffset.UtcNow;
        var initial=new SettingsState(1,"ja",true);
        string path=Path.Combine(directory,"settings.json");
        using(var seed=new DeferredSettings(path,()=>initial,now)) seed.Flush();
        long configBytes,configSaves;
        using(var settings=new DeferredSettings(path,()=>throw new Exception("Legacy called twice"),now))
        {
            for(int i=1;i<=8;i++) settings.Tick(now.AddSeconds(i*60));
            settings.Flush(); Check(settings.Saves==0 && settings.LogicalBytes==0,"Eight idle cycles: no saves/bytes");
            for(int i=0;i<120;i++) settings.Change(initial with {Language=i%2==0?"en":"de"});
            settings.Tick(now.AddSeconds(539)); Check(settings.Attempts==0,"Continuous edits wait until next 60-second boundary");
            settings.Tick(now.AddSeconds(540)); settings.Flush();
            Check(settings.Saves==1 && !settings.Dirty,"120 edits coalesced into one save");
            settings.Change(initial with {Language="fr"}); settings.Change(initial with {Language="de"});
            settings.Tick(now.AddSeconds(600)); settings.Flush(); Check(settings.Saves==1,"Reverting to saved values is write-free");
            settings.Change(initial with {Language="ko"});
            Parallel.For(0,16,_=>settings.Flush()); Check(settings.Saves==2,"Concurrent flushes share one committed value");
            settings.Change(initial with {Language="zh-Hans"}); settings.Dispose();
            Check(settings.Saves==3,"Exit flush saves last change");
            settings.Change(initial); settings.Tick(now.AddDays(1)); settings.Flush();
            Check(settings.Saves==3,"No changes or writes after disposal");
            configBytes=settings.LogicalBytes; configSaves=settings.Saves;
        }
        Check(Directory.GetFiles(directory,"settings.json.backup-*.json").Length==3,"Exactly three prior valid generations");
        File.WriteAllText(path,"broken");
        using(var recovered=new DeferredSettings(path,()=>throw new Exception("Legacy fallback unexpected"),now))
        {
            Check(recovered.Loaded.Language=="ko" && recovered.Recovered,"Corrupt primary restored from newest valid backup");
            Check(Directory.GetFiles(directory,"settings.json.corrupt-*").Length==1,"Damaged primary retained separately");
            Check(!recovered.Dirty,"Recovered primary matches loaded state");
        }
        string latest=Directory.GetFiles(directory,"settings.json.backup-*.json").OrderByDescending(x=>x).First();
        File.WriteAllText(latest,"broken backup");
        using(var valid=new DeferredSettings(path,()=>initial,now)) Check(valid.Loaded.Language=="ko" && !valid.Recovered,"Valid primary wins over backups");
        File.WriteAllText(path,"{}");
        using(var recovered=new DeferredSettings(path,()=>initial,now)) Check(recovered.Loaded.Language=="de","Corrupt newest backup skipped");

        string racing=Path.Combine(directory,"racing.json");
        using var entered=new ManualResetEventSlim(); using var release=new ManualResetEventSlim();
        bool block=true;
        using(var settings=new DeferredSettings(racing,()=>initial,now,stage=>
        { if(stage=="replace" && block) { entered.Set(); if(!release.Wait(5000)) throw new Exception("Test timed out"); } }))
        {
            settings.Tick(now.AddSeconds(60)); Check(entered.Wait(5000),"Save reached controlled in-flight boundary");
            settings.Change(initial with {Language="fr"}); block=false; release.Set(); settings.Flush();
            Check(settings.Saves==2 && !settings.Dirty,"Change during save is preserved and flushed");
            Check(JsonSerializer.Deserialize<SettingsState>(File.ReadAllText(racing))!.Language=="fr","Final file contains newer snapshot");
        }
        foreach(string failedStage in new[]{"write","replace"})
        {
            string brokenPath=Path.Combine(directory,"failure-"+failedStage+".json");
            using(var seed=new DeferredSettings(brokenPath,()=>initial,now)) seed.Flush();
            byte[] before=File.ReadAllBytes(brokenPath); bool fail=true;
            using var settings=new DeferredSettings(brokenPath,()=>initial,now,stage=> { if(fail && stage==failedStage) throw new IOException("Injected disk-full/rename failure"); });
            settings.Change(initial with {Language="en"});
            for(int i=1;i<=3;i++) { settings.Tick(now.AddSeconds(i*60)); int n=i; Wait(()=>settings.Attempts==n && settings.Error!=null && !settings.IsSaving,"Failure observed "+i); }
            settings.Tick(now.AddHours(8)); settings.Flush();
            Check(settings.Attempts==3 && settings.Stopped && settings.Dirty,"Failure capped at three attempts");
            Check(File.ReadAllBytes(brokenPath).SequenceEqual(before) && Directory.GetFiles(directory,Path.GetFileName(brokenPath)+".backup-*").Length==0,"Failed save preserves primary and generations");
            fail=false; settings.Retry(); settings.Flush(); Check(settings.Saves==1 && !settings.Dirty,"Explicit retry commits retained snapshot");
        }
        string locked=Path.Combine(directory,"locked.json");
        using(var seed=new DeferredSettings(locked,()=>initial,now)) seed.Flush();
        using(var settings=new DeferredSettings(locked,()=>initial,now))
        {
            settings.Change(initial with {Recording=false});
            using(var handle=new FileStream(locked,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            { settings.Flush(); Check(settings.Dirty && settings.Saves==0 && settings.Error!=null,"Real file lock retains dirty settings"); }
            settings.Flush(); Check(settings.Saves==1,"Unlocked file recovers");
        }
        long futureGeneration=DateTime.UtcNow.AddYears(1).Ticks;
        string oldBackup=Directory.GetFiles(directory,"locked.json.backup-*.json").Single();
        File.Move(oldBackup,locked+".backup-"+futureGeneration.ToString("D19")+"-clock-rollback.json");
        using(var settings=new DeferredSettings(locked,()=>initial,now))
        {
            settings.Change(initial with {Language="en",Recording=false}); settings.Flush();
            string newest=Directory.GetFiles(directory,"locked.json.backup-*.json").OrderByDescending(x=>x).First();
            Check(!newest.EndsWith("-clock-rollback.json"),"Backup generations remain ordered after clock rollback");
        }
        File.WriteAllText(locked,"broken");
        using(var restored=new DeferredSettings(locked,()=>initial,now))
            Check(!restored.Loaded.Recording && restored.Loaded.Language=="ja","Newest actual generation restores after clock rollback");

        var actor=new Character("sample","Synthetic","Alpha","Alpha");
        var midnight=new DateTimeOffset(new DateTime(2026,10,22,0,0,0,DateTimeKind.Local));
        Purchase P(string id,long seconds)=>new(id,seconds,actor,42,"Sample",false,2,100,10,"listing-"+id,"sample-retainer","Sample",1);
        string db=Path.Combine(directory,"market-ledger.db"); bool failArchive=true; int renames=0;
        long archiveBytes,archiveFiles,firstArchiveBytes,firstArchiveFiles,lateBytes;
        using(var store=new LedgerStore(db,stage=> { if(stage=="rename" && ++renames==3 && failArchive) throw new IOException("Injected archive rename failure"); }))
        {
            for(int i=0;i<6;i++) store.Insert(P(i.ToString(),midnight.AddMinutes(i-2).ToUnixTimeSeconds()));
            bool failed=false; try { store.ArchivePending(); } catch(IOException) { failed=true; }
            Check(failed && store.Query(new(),0,false).Totals.Total==1260,"Archive failure leaves all committed transactions queryable");
            failArchive=false; Check(store.ArchivePending(2)==2,"Partial migration restarts idempotently");
            firstArchiveBytes=store.Archive.LogicalBytes; firstArchiveFiles=store.Archive.FilesWritten;
        }
        using(var store=new LedgerStore(db))
        {
            while(store.ArchivePending(2)>0) { }
            Check(store.ArchiveReadRows==4,"Restart resumes only remaining rows");
            var dates=Directory.GetDirectories(store.ArchiveDirectory).Select(Path.GetFileName).Order().ToArray();
            Check(dates.SequenceEqual(new[]{"2026-10-21","2026-10-22"}),"Event timestamps partition across local midnight");
            long beforeFiles=store.Archive.FilesWritten,beforeBytes=store.Archive.LogicalBytes;
            var late=P("late",midnight.AddDays(-2).ToUnixTimeSeconds()); store.Insert(late); store.ArchivePending();
            lateBytes=store.Archive.LogicalBytes-beforeBytes;
            Check(store.Archive.FilesWritten-beforeFiles==1 && lateBytes<1024,"One new event writes one small file, not the existing history");
            Check(Directory.Exists(Path.Combine(store.ArchiveDirectory,"2026-10-20")),"Delayed/clock-rollback event goes to its own day");
            long writes=store.Archive.FilesWritten,bytes=store.Archive.LogicalBytes;
            Check(!store.Insert(late) && store.ArchivePending()==0,"Duplicate event and completed migration write nothing");
            Check(writes==store.Archive.FilesWritten && bytes==store.Archive.LogicalBytes,"No history rewrite after unchanged resume");
            var all=store.Query(new(),0,false); Check(all.Totals.Count==7 && all.Totals.Total==1470,"All periods remain queryable");
            long read=store.ReadStatements; for(int i=0;i<20;i++) store.Query(new(),0,false);
            Check(store.ReadStatements==read,"Unchanged all-date views use cached result");
            store.Export(new(),Path.Combine(directory,"all.csv"));
            Check(File.ReadLines(Path.Combine(directory,"all.csv")).Count()==8,"All-date CSV retains every record");
            using(var raw=new Sqlite(db))
            {
                using var state=raw.Prepare("SELECT records,total FROM gil_archive_state"); state.Step();
                Check(state.Int(0)==7 && state.Int(1)==1470,"Checkpoint totals match verified files and DB");
                using var rows=raw.Prepare("SELECT id,total FROM purchases ORDER BY time LIMIT 100"); int n=0; while(rows.Step()) n++;
                Check(n==7,"Legacy Allagan-style SQL still reads every record");
            }
            Directory.CreateDirectory(Path.Combine(directory,"exports")); File.WriteAllText(Path.Combine(directory,"exports","sample.csv"),"a,b");
            File.WriteAllText(Path.Combine(directory,"recovery-sample.json"),"[]");
            File.WriteAllText(db+".backup-test","backup");
            var measurement=HistoryFiles.Measure(directory,store.ArchiveDirectory);
            var included=Directory.GetFiles(directory).Where(x=>Path.GetFileName(x).StartsWith("market-ledger.db") || Path.GetFileName(x).StartsWith("recovery-"))
                .Concat(Directory.GetFiles(store.ArchiveDirectory,"*",SearchOption.AllDirectories)).Concat(Directory.GetFiles(Path.Combine(directory,"exports"))).ToArray();
            Check(measurement.Count==included.Length && measurement.Bytes==included.Sum(x=>new FileInfo(x).Length),"History measurement includes DB/WAL/SHM/backup/daily/recovery/CSV exact logical bytes");
            Check(measurement.Locations.Count==3,"Three explicit storage scopes");
            archiveBytes=store.Archive.LogicalBytes; archiveFiles=store.Archive.FilesWritten;
        }
        using(var reopened=new LedgerStore(db)) Check(reopened.ArchivePending()==0 && reopened.Archive.FilesWritten==0,"Completed migration restarts without files written");

        int attempts=0; bool pumpFails=true;
        using(var pump=new ArchivePump(()=> { Interlocked.Increment(ref attempts); if(pumpFails) throw new IOException("Injected archive failure"); return 0; },TimeSpan.FromMilliseconds(10)))
        {
            Wait(()=>pump.Stopped,"Archive pump stops after three failures");
            Check(attempts==3,"Exactly three archive attempts"); pump.Signal(); Thread.Sleep(50); Check(attempts==3,"New signals do not bypass failure stop");
            pumpFails=false; pump.Retry(); Wait(()=>attempts==4 && pump.Error==null,"Explicit archive retry recovers");
            pump.Dispose(); pump.Signal(); Thread.Sleep(30); Check(attempts==4,"No archive work after dispose");
        }
        using(var started=new ManualResetEventSlim())
        using(var finish=new ManualResetEventSlim())
        {
            int calls=0;
            var pump=new ArchivePump(()=> { Interlocked.Increment(ref calls); started.Set(); if(!finish.Wait(5000)) throw new Exception("Test gate timeout"); return 64; });
            Check(started.Wait(5000),"Archive batch started before shutdown");
            var stopping=Task.Run(pump.Dispose); Thread.Sleep(20);
            Check(!stopping.IsCompleted,"Shutdown joins in-progress archive write");
            finish.Set(); stopping.GetAwaiter().GetResult(); Thread.Sleep(30);
            Check(calls==1,"Shutdown checkpoints current batch and starts no further batch");
        }
        string exitFailure=Path.Combine(directory,"exit-failure.json");
        var unsaved=new DeferredSettings(exitFailure,()=>initial,now,_=>throw new IOException("Unavailable disk"));
        unsaved.Dispose(); int exitAttempts=unsaved.Attempts;
        unsaved.Flush(); unsaved.Tick(now.AddDays(1)); unsaved.Retry();
        Check(unsaved.Attempts==exitAttempts && unsaved.Dirty,"Failed exit does not leave a retrying writer after disposal");
        Console.WriteLine($"Persistence metrics: idle 8 cycles=0 saves/0 B; 120 edits=1 save; total edited settings={configSaves} saves/{configBytes} payload bytes; full archive including fault={archiveFiles+firstArchiveFiles} committed files/{archiveBytes+firstArchiveBytes} payload bytes; incremental late event=1 file/{lateBytes} B. These are logical payload counters, not physical SSD IO.");
        Console.WriteLine($"PASS: {checks} persistence, failure, migration, date, compatibility and shutdown checks. {directory}");
    }
}
