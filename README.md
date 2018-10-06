# Datové sady hlídače Státu - Insolvenční rejstřík

Program pro stažení záznamů z Insolvenčního rejstříku, jejich zpracování a uložení do datové sady Hlídače státu.

Podrobné informace o datovém zdroji v Hlídači Státu jsou [zde](https://www.hlidacstatu.cz/data/Index/insolvencni-rejstrik) včetně aktuálního datového schématu a možnosti prohlížení a vyhledávání v uložených datech.

## Jak spustit

Při spuštění je vyžadován API token, který lze získat po přihlášení na stránkách [Hlídače Státu - API pro vývojáře](https://www.hlidacstatu.cz/api/v1/Index).

API token lze zadat jako první parametr při spouštění programu

```
    InsolvencniRejstrik.exe apitokenapitokenapitokenapitoken
```

V případě jeho nezadání jako parametru Vás program po spuštění vyzve k jeho zadání ručně.

## Omezení

Program (zatím) čte pouze základní informace o jednotlivých řízeních, které jsou k dispozici se výsledcích vyhledávání. Pro získání doplňujících informací, jako je např. aktuální stav řízení, datum poslední změny či insolvenční správce, je nutné se dotazovat pro každé řízení samostatně. Což je omezeno limity na počet dotazů za den - řešení se hledá.