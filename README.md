# Datové sady Hlídače Státu - Insolvenční rejstřík

Program pro stažení záznamů z Insolvenčního rejstříku, jejich zpracování a uložení do Hlídače Státu. Podporuje dva způsoby stažení a uložení dat:
* stažení pomocí vyhledávače ISIR a uložení do datové sady
* zpracovávání změnových událostí a uložení do databáze

## Stažení pomocí vyhledávače ISIR a uložení do datové sady

Podrobné informace o datovém zdroji v Hlídači Státu jsou [zde](https://www.hlidacstatu.cz/data/Index/insolvencni-rejstrik) včetně aktuálního datového schématu a možnosti prohlížení a vyhledávání v uložených datech.

Seznam řízení v Insolvenčním rejstříku je získáván z vyhledávače [ISIR](https://isir.justice.cz/isir/common/index.do) odkud se postupně za vybrané období načítají vrácená řízení, pro které se následně volá webová služba [ISIR_CUZK_WS2](https://isir.justice.cz/isir/common/stat.do?kodStranky=SLEDOVANIWS) pro získání aktuálního stavu řízení a url adresy na detail řízení, který obsahuje podrobnější informace o řízení včetně všech dokumentů.

Pokud řízení bylo z rejstříku vyškrtnuto dle § 425 insolvenčního zákona, aktuální stav má hodnotu `ZNEPRISTUPNENO` a url adresa na detail řízení není vyplněna.

### Jak spustit

Program je potřeba spustit s přepínačem `-s` nebo `--search`, který definuje stahování záznamů z vyhledávače ISIR a ukládání do datové sady Hlídače Státu. Pro ukládání dat do datové sady je nutný API token, který lze získat po přihlášení na stránkách [Hlídače Státu - API pro vývojáře](https://www.hlidacstatu.cz/api/v1/Index) a který se zadává pomocí přepínače `--apitoken=<API TOKEN>`.

Volitelně je možné zadat také datum, od kterého se začnou záznamy načítat. V případě jeho nezadání se defaultně vyplní 1.1.2008, což je datum, kdy byl insolvenční rejstřík uveden do provozu a od kdy jsou v něm vedeny záznamy.

_spuštění s defaultním datem začátku_
```
    InsolvencniRejstrik.exe -s -apitoken=docela1dlouhy2token3pro4tohle5api
```

_spuštění s načítáním od 1.1.2018_
```
    InsolvencniRejstrik.exe -s -apitoken=docela1dlouhy2token3pro4tohle5api --date=1.1.2018
```


## Zpracovávání změnových událostí a uložení do databáze

Z Insolvenčního rejstříku lze pomocí služby ISIR_WS číst všechny změnové události ke všem řízení. Tyto události obsahují mimojiné informace o zahájení řízení, změny osob účastnících se řízení, změny stavu řízení a také informace o přidání dokumentu k řízení včetně linku pro jeho stažení.

Program v tomto režimu čte události od posledního v databázi uloženého ID a spravuje řízení, jejich osoby a navázané dokumenty a změny ukládá do databáze.

### Jak spustit

Program je potřeba spustit s přepínačem `-e` nebo `--events`, který definuje zpracování změnových událostí a jejich ukládání do databáze.

V případě, že nechcete číst a ukládat eventy do lokalní cache, použijte přepínač `--no-cache` a všechny dotazy budou pouze proti webovým službám Insolvenčního rejstříku. Pokud přepínač není zadán, nejprve se zkouší prohled lokální cache a až poté se provádí dotazy na webové služby Insolvenčního rejstříku. Každá událost vrácená z webové služby je zároven uložena do lokální cache (velikost lokální cache pro kompletní data může mít i několik desítek GB).

Pro urychlení zpracování doporučují inicializovat cache pro načítání odkazů na detaily jednotlivých řízení. Inicializace cache se provede pomocí přepínače `--init-link-cache` (velikost lokální cache odkazů je několik desítek MB)

_inicializace cache odkazů_
```
    InsolvencniRejstrik.exe ---init-link-cache
```

_spuštění_
```
    InsolvencniRejstrik.exe -e
```

_spuštění bez cache_
```
    InsolvencniRejstrik.exe -e --nocache
```

