Feature: MainPageFeature



Scenario: Main page is displayed
	Given i am logged in and on the main page
	When check elements existence
    | ElementLocator   | ElementTagName | AttributeName | ElementValue                    |
    | #dashboard img   | img            | src           | /images/icons/app-dashboard.svg |
    | #operations img  | img            | src           | /images/icons/app-receipt.svg   |
    | #categories img | img            | src           | /images/icons/app-tags.svg      |
		| #settings img        | img            | src           | /images/icons/app-settings.svg  |
    When check elements existence
    | ElementLocator                        | ElementTagName | AttributeName | ElementValue    |
    | .kpi-card.tone-income .kpi-title      | span           |               | Дохід           |
    | .kpi-card.tone-expense .kpi-title     | span           |               | Витрати         |
    | .kpi-card.tone-primary .kpi-title     | span           |               | Чистий прибуток |
    | .kpi-card.tone-neutral .kpi-title     | span           |               | Маржа           |
    


