namespace MarketLedger;

public static class Loc
{
    public static readonly string[] Codes = ["ja","en","de","fr","ko","zh-Hans","zh-Hant"];
    public static readonly Dictionary<string,string[]> Text = new()
    {
        ["History"]=["購入履歴","Purchases","Käufe","Achats","구매 내역","购买记录","購買紀錄"],
        ["Charts"]=["支出グラフ","Spending","Ausgaben","Dépenses","지출 그래프","支出图表","支出圖表"],
        ["Settings"]=["設定","Settings","Einstellungen","Paramètres","설정","设置","設定"],
        ["LanguageSettings"]=["言語設定","Language settings","Spracheinstellungen","Paramètres de langue","언어 설정","语言设置","語言設定"],
        ["Support"]=["支援","Support","Unterstützen","Soutien","후원","支持","支援"],
        ["Language"]=["言語 / Language","Language / 言語","Sprache / Language","Langue / Language","언어 / Language","语言 / Language","語言 / Language"],
        ["LanguageHelp"]=["変更はすぐに反映され、次回起動後も保持されます。","Changes apply immediately and remain after restarting.","Änderungen gelten sofort und bleiben nach dem Neustart erhalten.","Les changements sont immédiats et conservés après le redémarrage.","변경 사항은 즉시 적용되며 다시 시작해도 유지됩니다.","更改立即生效，并在重启后保留。","變更立即生效，並在重新啟動後保留。"],
        ["SupportText"]=["Roxyz0501 を任意で支援できます。","You can optionally support Roxyz0501.","Du kannst Roxyz0501 freiwillig unterstützen.","Vous pouvez soutenir Roxyz0501 si vous le souhaitez.","원한다면 Roxyz0501을 후원할 수 있습니다.","你可以自愿支持 Roxyz0501。","你可以自願支持 Roxyz0501。"],
        ["SupportHeading"]=["任意の支援","Optional support","Freiwillige Unterstützung","Soutien facultatif","선택적 후원","自愿支持","自願支持"],
        ["SupportRecipient"]=["支援先: Roxyz0501","Recipient: Roxyz0501","Empfänger: Roxyz0501","Bénéficiaire : Roxyz0501","후원 받는 사람: Roxyz0501","收款人：Roxyz0501","受款人：Roxyz0501"],
        ["OpenKoFi"]=["Ko-fi を開く","Open Ko-fi","Ko-fi öffnen","Ouvrir Ko-fi","Ko-fi 열기","打开 Ko-fi","開啟 Ko-fi"],
        ["CommandHelp"]=["Market Ledger の購入履歴を開きます。","Open Market Ledger purchases.","Market Ledger öffnen.","Ouvrir Market Ledger.","Market Ledger 구매 내역을 엽니다.","打开 Market Ledger 购买记录。","開啟 Market Ledger 購買紀錄。"],
        ["All"]=["すべて","All","Alle","Tous","전체","全部","全部"],
        ["Character"]=["キャラクター","Character","Charakter","Personnage","캐릭터","角色","角色"],
        ["Current"]=["現在のキャラ","Current character","Aktueller Charakter","Personnage actuel","현재 캐릭터","当前角色","目前角色"],
        ["Search"]=["アイテム・リテイナー名 / ID","Item / retainer name or ID","Gegenstand / Gehilfe / ID","Objet / servant / identifiant","아이템·집사 이름 / ID","物品·雇员名称 / ID","物品·雇員名稱 / ID"],
        ["World"]=["購入ワールド","Purchase world","Kaufwelt","Monde d’achat","구매 서버","购买服务器","購買伺服器"],
        ["Quality"]=["品質","Quality","Qualität","Qualité","품질","品质","品質"],
        ["From"]=["開始日 (YYYY-MM-DD)","From (YYYY-MM-DD)","Ab (YYYY-MM-DD)","Du (YYYY-MM-DD)","시작일 (YYYY-MM-DD)","开始日期 (YYYY-MM-DD)","開始日期 (YYYY-MM-DD)"],
        ["Until"]=["終了日 (YYYY-MM-DD)","Through (YYYY-MM-DD)","Bis (YYYY-MM-DD)","Au (YYYY-MM-DD)","종료일 (YYYY-MM-DD)","结束日期 (YYYY-MM-DD)","結束日期 (YYYY-MM-DD)"],
        ["Min"]=["税込金額 下限","Minimum total","Mindestbetrag inkl. Steuer","Total minimum TTC","세금 포함 최소 금액","含税金额下限","含稅金額下限"],
        ["Max"]=["税込金額 上限","Maximum total","Höchstbetrag inkl. Steuer","Total maximum TTC","세금 포함 최대 금액","含税金额上限","含稅金額上限"],
        ["Apply"]=["検索・適用","Search / apply","Suchen / anwenden","Rechercher / appliquer","검색·적용","搜索·应用","搜尋·套用"],
        ["Reset"]=["条件をリセット","Reset filters","Filter zurücksetzen","Réinitialiser les filtres","필터 초기화","重置筛选","重設篩選"],
        ["Today"]=["今日","Today","Heute","Aujourd’hui","오늘","今天","今天"],
        ["Month"]=["今月","This month","Dieser Monat","Ce mois","이번 달","本月","本月"],
        ["Last30"]=["直近30日","Last 30 days","Letzte 30 Tage","30 derniers jours","최근 30일","最近30天","最近30天"],
        ["AllTime"]=["全期間","All time","Gesamter Zeitraum","Toute la période","전체 기간","全部时间","全部時間"],
        ["DateError"]=["日付・金額・範囲を確認してください。","Check dates, amounts and ranges.","Datum, Beträge und Bereiche prüfen.","Vérifiez les dates, montants et intervalles.","날짜, 금액, 범위를 확인하세요.","请检查日期、金额和范围。","請檢查日期、金額和範圍。"],
        ["Time"]=["購入日時","Purchased","Kaufdatum","Date d’achat","구매 일시","购买时间","購買時間"],
        ["Item"]=["アイテム","Item","Gegenstand","Objet","아이템","物品","物品"],
        ["Quantity"]=["数量","Quantity","Menge","Quantité","수량","数量","數量"],
        ["Unit"]=["単価（税抜）","Unit price (before tax)","Stückpreis ohne Steuer","Prix unitaire HT","단가 (세전)","单价（税前）","單價（稅前）"],
        ["Subtotal"]=["税抜","Before tax","Ohne Steuer","Hors taxes","세전","税前","稅前"],
        ["Tax"]=["税額","Tax","Steuer","Taxes","세금","税额","稅額"],
        ["Total"]=["税込","Including tax","Mit Steuer","TTC","세금 포함","含税","含稅"],
        ["Retainer"]=["リテイナー","Retainer","Gehilfe","Servant","집사","雇员","雇員"],
        ["Unknown"]=["不明","Unknown","Unbekannt","Inconnu","알 수 없음","未知","未知"],
        ["Count"]=["購入件数","Purchases","Anzahl Käufe","Nombre d’achats","구매 건수","购买次数","購買次數"],
        ["Previous"]=["前へ","Previous","Zurück","Précédent","이전","上一页","上一頁"],
        ["Next"]=["次へ","Next","Weiter","Suivant","다음","下一页","下一頁"],
        ["Page"]=["ページ","Page","Seite","Page","페이지","页","頁"],
        ["NoData"]=["この条件の購入履歴はありません。","No purchases match these filters.","Keine passenden Käufe.","Aucun achat ne correspond aux filtres.","조건에 맞는 구매 내역이 없습니다.","没有符合条件的购买记录。","沒有符合條件的購買紀錄。"],
        ["Loading"]=["読み込み中…","Loading…","Wird geladen…","Chargement…","불러오는 중…","加载中…","載入中…"],
        ["Export"]=["検索結果をCSV出力","Export filtered CSV","Gefilterte CSV exportieren","Exporter les résultats en CSV","검색 결과 CSV 내보내기","导出筛选结果CSV","匯出篩選結果CSV"],
        ["Exporting"]=["CSV出力中…","Exporting CSV…","CSV wird exportiert…","Export CSV en cours…","CSV 내보내는 중…","正在导出CSV…","正在匯出CSV…"],
        ["Exported"]=["CSVを保存しました","CSV saved","CSV gespeichert","CSV enregistré","CSV 저장 완료","CSV已保存","CSV已儲存"],
        ["Folder"]=["保存フォルダーを開く","Open data folder","Datenordner öffnen","Ouvrir le dossier","저장 폴더 열기","打开保存文件夹","開啟儲存資料夾"],
        ["ReadError"]=["履歴を読み込めません。再試行してください。","Could not read the ledger. Please retry.","Lesen fehlgeschlagen. Bitte erneut versuchen.","Lecture impossible. Réessayez.","내역을 읽지 못했습니다. 다시 시도하세요.","无法读取记录，请重试。","無法讀取紀錄，請重試。"],
        ["WriteError"]=["未保存の購入があります。空き容量とログを確認し、再保存してください。","Some purchases are not saved. Check disk space and logs, then retry saving.","Käufe nicht gespeichert. Speicherplatz und Protokoll prüfen, dann erneut speichern.","Des achats ne sont pas enregistrés. Vérifiez l’espace disque et les journaux, puis réessayez.","저장되지 않은 구매가 있습니다. 디스크 공간과 로그를 확인한 후 다시 저장하세요.","部分购买未保存。请检查磁盘空间和日志，然后重新保存。","部分購買未儲存。請檢查磁碟空間和記錄，再重新儲存。"],
        ["Retry"]=["再試行","Retry","Erneut versuchen","Réessayer","다시 시도","重试","重試"],
        ["ExportError"]=["CSV出力に失敗しました。空き容量とログを確認してください。","CSV export failed. Check disk space and logs.","CSV-Export fehlgeschlagen. Speicherplatz und Protokoll prüfen.","Échec de l’export CSV. Vérifiez l’espace disque et les journaux.","CSV 내보내기에 실패했습니다. 디스크 공간과 로그를 확인하세요.","CSV导出失败，请检查磁盘空间和日志。","CSV匯出失敗，請檢查磁碟空間和記錄。"],
        ["Daily"]=["日別","Daily","Täglich","Par jour","일별","每日","每日"],
        ["Monthly"]=["月別","Monthly","Monatlich","Par mois","월별","每月","每月"],
        ["Cumulative"]=["累積表示","Cumulative","Kumuliert","Cumul","누적 표시","累计显示","累計顯示"],
        ["GraphNote"]=["検索条件に一致する直近366区間。日付はPCの現地時刻、累積は表示区間内の合計です。","Latest 366 matching periods. Dates use local PC time; cumulative totals cover the displayed periods.","Letzte 366 passende Zeiträume. Lokale PC-Zeit; kumulierte Werte gelten für angezeigte Zeiträume.","366 dernières périodes correspondantes. Heure locale du PC ; cumul des périodes affichées.","조건에 맞는 최근 366구간. 날짜는 PC 현지 시간이며 누적은 표시된 구간의 합계입니다.","最近366个符合条件的时段。日期使用电脑本地时间，累计值仅含显示时段。","最近366個符合條件的時段。日期使用電腦本地時間，累計值僅含顯示時段。"],
        ["Recording"]=["購入履歴を記録する","Record purchases","Käufe aufzeichnen","Enregistrer les achats","구매 내역 기록","记录购买历史","記錄購買歷史"],
        ["Limitations"]=["有効中に成功した購入から記録します。導入前や停止中の履歴は復元できません。取得できなかったリテイナー名は不明として保存します。","Records successful purchases while enabled. Earlier or paused purchases cannot be recovered. Unavailable retainer names are stored as unknown.","Erfasst erfolgreiche Käufe während der Aktivierung. Frühere Käufe sind nicht wiederherstellbar. Fehlende Gehilfennamen bleiben unbekannt.","Enregistre les achats réussis lorsque le suivi est actif. Les achats antérieurs ne sont pas récupérables. Les noms de servants indisponibles restent inconnus.","기록이 켜진 동안 성공한 구매만 저장합니다. 설치 전이나 중지 중의 내역은 복원할 수 없습니다. 얻지 못한 집사 이름은 알 수 없음으로 저장합니다.","仅记录启用期间成功的购买。无法恢复安装前或暂停期间的记录，无法获取的雇员名标为未知。","僅記錄啟用期間成功的購買。無法還原安裝前或暫停期間的紀錄，無法取得的雇員名標為未知。"],
        ["LocalOnly"]=["データはこのPC内だけに保存します。CSVは選択中の検索条件の全ページを出力します。","Data stays on this PC. CSV exports all pages matching the applied filters.","Daten bleiben auf diesem PC. CSV enthält alle Seiten der angewendeten Filter.","Les données restent sur ce PC. Le CSV inclut toutes les pages correspondant aux filtres appliqués.","데이터는 이 PC에만 저장합니다. CSV는 적용된 조건에 맞는 모든 페이지를 내보냅니다.","数据仅保存在本机。CSV导出当前已应用筛选条件的所有页面。","資料僅儲存在本機。CSV匯出目前已套用篩選條件的所有頁面。"],
        ["Unmatched"]=["照合できなかった通知","Unmatched notifications","Nicht zugeordnete Meldungen","Notifications non rapprochées","일치하지 않는 알림","未匹配的通知","未配對的通知"],
        ["Paused"]=["記録停止中","Recording paused","Aufzeichnung pausiert","Enregistrement suspendu","기록 중지됨","记录已暂停","記錄已暫停"],
        ["Newest"]=["新しい順","Newest first","Neueste zuerst","Plus récents","최신순","最新优先","最新優先"],
        ["Oldest"]=["古い順","Oldest first","Älteste zuerst","Plus anciens","오래된순","最早优先","最早優先"],
        ["Highest"]=["高額順","Highest total","Höchster Betrag","Total décroissant","높은 금액순","金额降序","金額降冪"],
        ["Lowest"]=["低額順","Lowest total","Niedrigster Betrag","Total croissant","낮은 금액순","金额升序","金額升冪"],
        ["Sort"]=["並び順","Sort","Sortierung","Tri","정렬","排序","排序"],
        ["Applied"]=["表示・CSVは「検索・適用」で確定した条件が対象です。","Display and CSV use filters confirmed with Search / apply.","Anzeige und CSV verwenden die mit Suchen / anwenden bestätigten Filter.","L’affichage et le CSV utilisent les filtres validés par Rechercher / appliquer.","화면과 CSV에는 검색·적용으로 확정한 조건이 적용됩니다.","显示和CSV使用点击搜索·应用后确认的条件。","顯示和CSV使用點擊搜尋·套用後確認的條件。"],
        ["Refresh"]=["再読み込み","Reload","Neu laden","Recharger","다시 불러오기","重新加载","重新載入"],
        ["Recover"]=["未保存データを復元","Recover unsaved purchases","Ungespeicherte Käufe wiederherstellen","Récupérer les achats non enregistrés","미저장 구매 복원","恢复未保存购买","還原未儲存購買"],
        ["RecoveryFiles"]=["復元待ちファイル","Recovery files","Wiederherstellungsdateien","Fichiers de récupération","복원 파일","待恢复文件","待還原檔案"],
    };
    public static string Get(string? code,string key)
    {
        int index=Array.IndexOf(Codes,code);
        return Text.TryGetValue(key,out var values)?values[index<0?1:index]:key;
    }
    public static string? Normalize(string? raw)
    {
        var value = raw?.Trim().Replace('_','-').ToLowerInvariant();
        if (value is "zh-hans" or "zh-cn" or "zh-sg") return "zh-Hans";
        if (value is "zh-hant" or "zh-tw" or "zh-hk" or "zh-mo") return "zh-Hant";
        return value?.Split('-')[0] is string simple && Codes.Take(5).Contains(simple) ? simple : null;
    }
    public static string Resolve(string? saved,string? game,string? ui) => Normalize(saved) ?? Normalize(game) ?? Normalize(ui) ?? "en";
    public static string FontText => string.Concat(Text.Values.SelectMany(x=>x)) + "日本語EnglishDeutschFrançais한국어简体中文繁體中文—…";
    public static void Validate()
    {
        foreach(var pair in Text) if(pair.Value.Length!=7 || pair.Value.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException(pair.Key);
    }
}
