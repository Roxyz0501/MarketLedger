namespace MarketLedger;

public static class Loc
{
    public static readonly string[] Codes = ["ja","en","de","fr","ko","zh-Hans","zh-Hant"];
    public static readonly Dictionary<string,string[]> Text = new()
    {
        ["History"]=["支出履歴","Spending history","Ausgabenverlauf","Historique des dépenses","지출 내역","支出记录","支出紀錄"],
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
        ["CommandHelp"]=["Gil Ledger の支出履歴を開きます。","Open Gil Ledger spending history.","Gil Ledger öffnen.","Ouvrir Gil Ledger.","Gil Ledger 지출 내역을 엽니다.","打开 Gil Ledger 支出记录。","開啟 Gil Ledger 支出紀錄。"],
        ["All"]=["すべて","All","Alle","Tous","전체","全部","全部"],
        ["Character"]=["キャラクター","Character","Charakter","Personnage","캐릭터","角色","角色"],
        ["Current"]=["現在のキャラ","Current character","Aktueller Charakter","Personnage actuel","현재 캐릭터","当前角色","目前角色"],
        ["Search"]=["アイテム・販売元 / ID","Item / seller / ID","Gegenstand / Verkäufer / ID","Objet / vendeur / identifiant","아이템·판매처 / ID","物品·卖家 / ID","物品·賣家 / ID"],
        ["World"]=["ワールド","World","Welt","Monde","서버","服务器","伺服器"],
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
        ["Time"]=["記録日時","Recorded","Erfasst am","Date de l’écriture","기록 일시","记录时间","紀錄時間"],
        ["Item"]=["アイテム","Item","Gegenstand","Objet","아이템","物品","物品"],
        ["Quantity"]=["数量","Quantity","Menge","Quantité","수량","数量","數量"],
        ["Unit"]=["単価（税抜）","Unit price (before tax)","Stückpreis ohne Steuer","Prix unitaire HT","단가 (세전)","单价（税前）","單價（稅前）"],
        ["Subtotal"]=["税抜","Before tax","Ohne Steuer","Hors taxes","세전","税前","稅前"],
        ["Tax"]=["税額","Tax","Steuer","Taxes","세금","税额","稅額"],
        ["Total"]=["支払・減少額（税込）","Paid / decrease (incl. tax)","Zahlung / Abgang (inkl. Steuer)","Paiement / diminution (TTC)","지불·감소액 (세금 포함)","支付·减少金额（含税）","支付·減少金額（含稅）"],
        ["Retainer"]=["リテイナー","Retainer","Gehilfe","Servant","집사","雇员","雇員"],
        ["Unknown"]=["不明","Unknown","Unbekannt","Inconnu","알 수 없음","未知","未知"],
        ["Count"]=["記録件数","Records","Einträge","Écritures","기록 수","记录数","紀錄數"],
        ["Previous"]=["前へ","Previous","Zurück","Précédent","이전","上一页","上一頁"],
        ["Next"]=["次へ","Next","Weiter","Suivant","다음","下一页","下一頁"],
        ["Page"]=["ページ","Page","Seite","Page","페이지","页","頁"],
        ["NoData"]=["この条件の記録はありません。","No records match these filters.","Keine passenden Einträge.","Aucune écriture ne correspond aux filtres.","조건에 맞는 기록이 없습니다.","没有符合条件的记录。","沒有符合條件的紀錄。"],
        ["Loading"]=["読み込み中…","Loading…","Wird geladen…","Chargement…","불러오는 중…","加载中…","載入中…"],
        ["Export"]=["検索結果をCSV出力","Export filtered CSV","Gefilterte CSV exportieren","Exporter les résultats en CSV","검색 결과 CSV 내보내기","导出筛选结果CSV","匯出篩選結果CSV"],
        ["Exporting"]=["CSV出力中…","Exporting CSV…","CSV wird exportiert…","Export CSV en cours…","CSV 내보내는 중…","正在导出CSV…","正在匯出CSV…"],
        ["Exported"]=["CSVを保存しました","CSV saved","CSV gespeichert","CSV enregistré","CSV 저장 완료","CSV已保存","CSV已儲存"],
        ["Folder"]=["保存フォルダーを開く","Open data folder","Datenordner öffnen","Ouvrir le dossier","저장 폴더 열기","打开保存文件夹","開啟儲存資料夾"],
        ["ReadError"]=["履歴を読み込めません。再試行してください。","Could not read the ledger. Please retry.","Lesen fehlgeschlagen. Bitte erneut versuchen.","Lecture impossible. Réessayez.","내역을 읽지 못했습니다. 다시 시도하세요.","无法读取记录，请重试。","無法讀取紀錄，請重試。"],
        ["WriteError"]=["未保存の記録があります。空き容量とログを確認し、再保存してください。","Some records are not saved. Check disk space and logs, then retry saving.","Einträge nicht gespeichert. Speicherplatz und Protokoll prüfen, dann erneut speichern.","Des écritures ne sont pas enregistrées. Vérifiez l’espace disque et les journaux, puis réessayez.","저장되지 않은 기록이 있습니다. 디스크 공간과 로그를 확인한 후 다시 저장하세요.","部分记录未保存。请检查磁盘空间和日志，然后重新保存。","部分紀錄未儲存。請檢查磁碟空間和記錄，再重新儲存。"],
        ["Retry"]=["再試行","Retry","Erneut versuchen","Réessayer","다시 시도","重试","重試"],
        ["ExportError"]=["CSV出力に失敗しました。空き容量とログを確認してください。","CSV export failed. Check disk space and logs.","CSV-Export fehlgeschlagen. Speicherplatz und Protokoll prüfen.","Échec de l’export CSV. Vérifiez l’espace disque et les journaux.","CSV 내보내기에 실패했습니다. 디스크 공간과 로그를 확인하세요.","CSV导出失败，请检查磁盘空间和日志。","CSV匯出失敗，請檢查磁碟空間和記錄。"],
        ["Daily"]=["日別","Daily","Täglich","Par jour","일별","每日","每日"],
        ["Monthly"]=["月別","Monthly","Monatlich","Par mois","월별","每月","每月"],
        ["Cumulative"]=["累積表示","Cumulative","Kumuliert","Cumul","누적 표시","累计显示","累計顯示"],
        ["GraphNote"]=["検索条件に一致する直近366区間。日付はPCの現地時刻、累積は表示区間内の合計です。","Latest 366 matching periods. Dates use local PC time; cumulative totals cover the displayed periods.","Letzte 366 passende Zeiträume. Lokale PC-Zeit; kumulierte Werte gelten für angezeigte Zeiträume.","366 dernières périodes correspondantes. Heure locale du PC ; cumul des périodes affichées.","조건에 맞는 최근 366구간. 날짜는 PC 현지 시간이며 누적은 표시된 구간의 합계입니다.","最近366个符合条件的时段。日期使用电脑本地时间，累计值仅含显示时段。","最近366個符合條件的時段。日期使用電腦本地時間，累計值僅含顯示時段。"],
        ["Recording"]=["購入・ギル減少を記録する","Record purchases and gil decreases","Käufe und Gil-Abgänge erfassen","Enregistrer achats et diminutions de gils","구매·길 감소 기록","记录购买和金币减少","記錄購買和金幣減少"],
        ["Limitations"]=["有効中のマーケット・通常のNPCギルショップ購入を記録します。導入前や停止中の履歴は復元できません。特殊通貨・交換ショップ・買い戻しは購入明細の対象外です。その他のギル減少は最大30秒後に未分類で記録します。未分類には移動・手数料なども含まれ、税額は不明です。","Records market and ordinary NPC gil-shop purchases while enabled. Earlier history cannot be recovered. Special currencies, exchange shops and buyback lack purchase details. Other gil decreases appear as unclassified after up to 30 seconds; they may include transfers or fees, and their tax is unknown.","Erfasst Markt- und normale NPC-Gil-Käufe. Frühere Daten sind nicht wiederherstellbar. Sonderwährungen, Tausch und Rückkauf ohne Kaufdetails. Sonstige Gil-Abgänge erscheinen nach bis zu 30 Sekunden als unklassifiziert; sie können Übertragungen oder Gebühren enthalten. Steuer unbekannt.","Enregistre les achats au marché et dans les boutiques PNJ ordinaires en gils. Historique antérieur irrécupérable. Devises spéciales, échanges et rachats sans détail. Les autres diminutions sont non classées après 30 secondes au plus ; transferts ou frais possibles, taxes inconnues.","활성화 중의 장터·일반 NPC 길 상점 구매를 기록합니다. 이전 내역은 복원할 수 없습니다. 특수 화폐·교환 상점·되사기는 구매 상세 대상이 아닙니다. 기타 길 감소는 최대 30초 후 미분류로 기록되며 이동·수수료 등이 포함될 수 있고 세금은 알 수 없습니다.","记录启用期间的市场及普通NPC金币商店购买，无法恢复之前的历史。特殊货币、兑换商店和回购不提供购买明细。其他金币减少最多30秒后记为未分类，可能包含转移或费用，税额未知。","記錄啟用期間的市場及普通NPC金幣商店購買，無法還原之前的歷史。特殊貨幣、兌換商店和回購不提供購買明細。其他金幣減少最多30秒後記為未分類，可能包含轉移或費用，稅額未知。"],
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
        ["Recover"]=["未保存データを復元","Recover unsaved records","Ungespeicherte Einträge wiederherstellen","Récupérer les écritures non enregistrées","미저장 기록 복원","恢复未保存记录","還原未儲存紀錄"],
        ["RecoveryFiles"]=["復元待ちファイル","Recovery files","Wiederherstellungsdateien","Fichiers de récupération","복원 파일","待恢复文件","待還原檔案"],
        ["Source"]=["支出種別","Source","Art","Type","지출 유형","支出类型","支出類型"],
        ["Confirmed"]=["購入（確認済み）","Confirmed purchases","Bestätigte Käufe","Achats confirmés","확인된 구매","已确认购买","已確認購買"],
        ["Market"]=["マーケット","Market board","Marktbrett","Tableau des ventes","장터","市场","市場"],
        ["NpcShop"]=["NPC購入","NPC purchase","NPC-Kauf","Achat PNJ","NPC 구매","NPC购买","NPC購買"],
        ["Unclassified"]=["未分類のギル減少","Unclassified gil decrease","Unklassifizierter Gil-Abgang","Diminution de gils non classée","미분류 길 감소","未分类金币减少","未分類金幣減少"],
        ["Transfer"]=["資金移動","Gil transfer","Gil-Übertragung","Transfert de gils","길 이동","金币转移","金幣轉移"],
        ["AllOutflows"]=["すべての記録","All records","Alle Einträge","Toutes les écritures","모든 기록","所有记录","所有紀錄"],
        ["Seller"]=["販売元 / 店舗","Seller / shop","Verkäufer / Geschäft","Vendeur / boutique","판매처·상점","卖家·商店","賣家·商店"],
        ["SourceHelp"]=["通常は確認済みの購入のみ集計します。未分類・資金移動は種別で選ぶと表示できます。","Totals normally include confirmed purchases only. Select unclassified decreases or transfers to view them.","Summen umfassen normalerweise nur bestätigte Käufe. Unklassifizierte Abgänge und Übertragungen separat auswählen.","Les totaux incluent normalement les achats confirmés. Sélectionnez séparément diminutions non classées et transferts.","기본 합계에는 확인된 구매만 포함됩니다. 미분류 감소·길 이동은 유형을 선택하면 볼 수 있습니다.","默认仅统计已确认购买。选择对应类型可查看未分类减少或金币转移。","預設僅統計已確認購買。選擇對應類型可查看未分類減少或金幣轉移。"],
        ["UnknownTaxTotal"]=["税内訳不明の減少額（税抜・税額には含めません）","Decrease with unknown tax (excluded from before-tax and tax sums)","Abgang mit unbekannter Steuer (ohne Aufteilung)","Diminution aux taxes inconnues (exclue des sommes HT et taxes)","세금 내역 불명 감소액 (세전·세금 합계에서 제외)","税额未知的减少金额（不计入税前和税额合计）","稅額未知的減少金額（不計入稅前和稅額合計）"],
        ["NpcUnavailable"]=["NPC購入の検出を開始できませんでした。NPCのギル減少は未分類で記録します。","NPC purchase detection is unavailable. NPC gil decreases will be unclassified.","NPC-Kauferkennung nicht verfügbar. Gil-Abgänge bleiben unklassifiziert.","Détection des achats PNJ indisponible. Les diminutions seront non classées.","NPC 구매 감지를 시작하지 못했습니다. NPC 길 감소는 미분류로 기록됩니다.","无法启用NPC购买检测，相关金币减少将记为未分类。","無法啟用NPC購買偵測，相關金幣減少將記為未分類。"],
        ["Overflow"]=["短時間の変更が多く、一部の照合を省略しました。記録が不完全な可能性があります。","Too many changes arrived at once; some matches were skipped. Records may be incomplete.","Zu viele Änderungen gleichzeitig; einige Zuordnungen fehlen. Aufzeichnung möglicherweise unvollständig.","Trop de changements simultanés ; certains rapprochements ont été ignorés. Historique potentiellement incomplet.","변경이 너무 많아 일부 대조를 생략했습니다. 기록이 불완전할 수 있습니다.","短时间内变化过多，已跳过部分匹配，记录可能不完整。","短時間內變化過多，已略過部分配對，紀錄可能不完整。"],
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
