using System.Runtime.InteropServices;

namespace MarketLedger;

// Uses Windows' serviced SQLite library; no native binary is bundled or downloaded.
internal sealed class Sqlite : IDisposable
{
    private nint db;
    public Sqlite(string path)
    {
        int rc = Native.sqlite3_open_v2(path, out db, 0x2 | 0x4 | 0x10000, 0);
        if (rc != 0) { var error = Error(); Dispose(); throw new IOException(error); }
        Native.sqlite3_busy_timeout(db, 5000);
        if(Native.sqlite3_create_function_v2(db,"ledger_day",1,1,0,DayFunction,0,0,0)!=0)
        { var error=Error(); Dispose(); throw new IOException(error); }
    }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Scalar(nint context,int count,nint arguments);
    private static readonly Scalar DayFunction=LocalDay;
    private static void LocalDay(nint context,int count,nint arguments)
    {
        try
        {
            long seconds=Native.sqlite3_value_int64(Marshal.ReadIntPtr(arguments));
            string day=DateTimeOffset.FromUnixTimeSeconds(seconds).ToLocalTime().ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
            Native.sqlite3_result_text(context,day,-1,new nint(-1));
        }
        catch { Native.sqlite3_result_error(context,"Invalid ledger timestamp",-1); }
    }
    private string Error() => Marshal.PtrToStringUTF8(Native.sqlite3_errmsg(db)) ?? "SQLite error";
    public Statement Prepare(string sql, params object[] values)
    {
        if (Native.sqlite3_prepare_v2(db, sql, -1, out var statement, 0) != 0) throw new IOException(Error());
        var result = new Statement(this, statement);
        try { for (int i = 0; i < values.Length; i++) result.Bind(i + 1, values[i]); }
        catch { result.Dispose(); throw; }
        return result;
    }
    public void Execute(string sql, params object[] values) { using var s = Prepare(sql, values); while (s.Step()) { } }
    public void Dispose() { if (db != 0) { Native.sqlite3_close_v2(db); db = 0; } }
    internal sealed class Statement(Sqlite owner, nint handle) : IDisposable
    {
        public void Bind(int index, object value)
        {
            int rc = value is string text ? Native.sqlite3_bind_text(handle, index, text, -1, new nint(-1))
                : Native.sqlite3_bind_int64(handle, index, Convert.ToInt64(value));
            if (rc != 0) throw new IOException(owner.Error());
        }
        public bool Step()
        {
            int rc = Native.sqlite3_step(handle);
            if (rc == 100) return true;
            if (rc == 101) return false;
            throw new IOException(owner.Error());
        }
        public long Int(int column) => Native.sqlite3_column_int64(handle, column);
        public string Text(int column) => Marshal.PtrToStringUTF8(Native.sqlite3_column_text(handle, column)) ?? "";
        public void Dispose() { if (handle != 0) { Native.sqlite3_finalize(handle); handle = 0; } }
    }
    private static class Native
    {
        private const string Dll = "winsqlite3.dll";
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_create_function_v2(nint db,[MarshalAs(UnmanagedType.LPUTF8Str)] string name,int count,int encoding,nint user,Scalar callback,nint step,nint final,nint destroy);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern long sqlite3_value_int64(nint value);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void sqlite3_result_text(nint context,[MarshalAs(UnmanagedType.LPUTF8Str)] string value,int length,nint destructor);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void sqlite3_result_error(nint context,[MarshalAs(UnmanagedType.LPUTF8Str)] string value,int length);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string name, out nint db, int flags, nint vfs);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_close_v2(nint db);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_busy_timeout(nint db, int ms);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern nint sqlite3_errmsg(nint db);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_prepare_v2(nint db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int bytes, out nint stmt, nint tail);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_bind_text(nint stmt, int index, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, int bytes, nint destructor);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_bind_int64(nint stmt, int index, long value);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_step(nint stmt);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern long sqlite3_column_int64(nint stmt, int index);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern nint sqlite3_column_text(nint stmt, int index);
        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_finalize(nint stmt);
    }
}
