# Datové sady Hlídače Státu - Insolvenční rejstřík

Program pro stažení záznamů z Insolvenčního rejstříku, jejich zpracování a uložení do datové sady Hlídače Státu.

Podrobné informace o datovém zdroji v Hlídači Státu jsou [zde](https://www.hlidacstatu.cz/data/Index/insolvencni-rejstrik) včetně aktuálního datového schématu a možnosti prohlížení a vyhledávání v uložených datech.

Seznam řízení v Insolvenčním rejstříku je získáván z vyhledávače [ISIR](https://isir.justice.cz/isir/common/index.do) odkud se postupně za vybrané období načítají vrácená řízení, pro které se následně volá webová služba [ISIR_CUZK_WS2](https://isir.justice.cz/isir/common/stat.do?kodStranky=SLEDOVANIWS) pro získání aktuálního stavu řízení a url adresy na detail řízení, který obsahuje podrobnější informace o řízení včetně všech dokumentů.

Pokud řízení bylo z rejstříku vyškrtnuto dle § 425 insolvenčního zákona, aktuální stav má hodnotu `ZNEPRISTUPNENO` a url adresa na detail řízení není vyplněna.

## Jak spustit

Při spuštění je vyžadován API token, který lze získat po přihlášení na stránkách [Hlídače Státu - API pro vývojáře](https://www.hlidacstatu.cz/api/v1/Index).

API token lze zadat jako první parametr při spouštění programu

```
    InsolvencniRejstrik.exe apitokenapitokenapitokenapitoken
```

V případě jeho nezadání jako parametru Vás program po spuštění vyzve k jeho zadání ručně.

