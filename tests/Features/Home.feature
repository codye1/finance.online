Feature: Home dashboard

Background:
    Given I am logged in and on the home page

# ---------- Навігація ----------

Scenario: Navigation menu leads to the correct pages
    When check navigation
        | ElementLocator | ExpectedUrl |
        | #operations    | /operations |
        | #categories    | /categories |
        | #settings      | /settings   |

# ---------- KPI ----------

Scenario: KPI cards are displayed
    Then check elements count
        | ElementLocator | ExpectedCount |
        | .kpi-card      | 4             |

# ---------- Адаптивність ----------

Scenario Outline: Mobile net balance depends on viewport width (<Width>px)
    Given the viewport size is <Width>x<Height>
    Then check elements visibility
        | ElementLocator                       | Visible   |
        | header span:text-is('Чистий баланс') | <Visible> |

    Examples:
        | Width | Height | Visible |
        | 375   | 812    | true    |
        | 767   | 1024   | true    |
        | 768   | 1024   | false   |
        | 1440  | 900    | false   |

# ---------- Dropdown-и (організація / період) ----------

Scenario Outline: Header dropdown opens and closes (<Toggle>)
    When click element "<Toggle>"
    Then check elements visibility
        | ElementLocator | Visible |
        | <Dropdown>     | true    |
    When click element "<Toggle>"
    Then check elements visibility
        | ElementLocator | Visible |
        | <Dropdown>     | false   |

    Examples:
        | Toggle                | Dropdown                |
        | #org-select-toggle    | #org-select-dropdown    |
        | #period-select-toggle | #period-select-dropdown |

# ---------- Перемикач періоду ----------

Scenario Outline: Period switcher changes the label and the url (<Label>)
    When click elements
        | ElementLocator                                             |
        | #period-select-toggle                                      |
        | #period-select-dropdown .org-select-item:text-is('<Label>') |
    Then current url contains "period=<Value>"
    And check elements contain text
        | ElementLocator        | ExpectedText |
        | #period-select-toggle | <Label>      |

    Examples:
        | Label   | Value |
        | Тиждень | week  |
        | Місяць  | month |
        | Рік     | year  |

# ---------- Модалка "Нова операція" ----------

Scenario: Open and close the add operation modal
    When click elements
        | ElementLocator            |
        | #open-operation-modal-btn |
    Then check elements visibility
        | ElementLocator      | Visible |
        | #form-add-operation | true    |
    When click elements
        | ElementLocator            |
        | #js-close-operation-modal |
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false    |

# Порожні поля jQuery Validate перевіряє лише при сабміті -> тест клікає submit
Scenario Outline: Add operation form validation on submit - <Case>
    Given I open the add operation modal
    When fill form
        | ElementLocator        | Action | Value           |
        | #operation-amount     | fill   | <Amount>        |
        | #operation-category   | select | <CategoryIndex> |
        | #operation-date       | fill   | {today}         |
        | #btn-submit-operation | click  |                 |
    Then check elements existence
        | ElementLocator                                                     | ElementTagName | AttributeName | ElementValue |
        | #form-add-operation .form-group:has(<Field>) span.form-error-text | span           |               | <Error>      |

    Examples:
        | Case           | Field               | Amount | CategoryIndex | Error           |
        | empty amount   | #operation-amount   |        | 1             | Вкажіть суму    |
        | empty category | #operation-category | 100    | 0             | Оберіть категорію |

# Непорожнє некоректне значення валідується при blur -> помилка з'являється одразу,
# а кнопка submit стає disabled, тому кліку немає
Scenario Outline: Add operation form validation on blur - <Case>
    Given I open the add operation modal
    When fill form
        | ElementLocator      | Action | Value    |
        | #operation-amount   | fill   | <Amount> |
        | #operation-category | select | 1        |
        | #operation-date     | fill   | {today}  |
    Then check elements existence
        | ElementLocator                                                            | ElementTagName | AttributeName | ElementValue                |
        | #form-add-operation .form-group:has(#operation-amount) span.form-error-text | span           |               | Сума повинна бути більше 0 |
    And check elements state
        | ElementLocator        | Enabled |
        | #btn-submit-operation | false   |

    Examples:
        | Case            | Amount |
        | zero amount     | 0      |
        | negative amount | -50    |

Scenario: Successfully add a new expense operation and KPI values change
    Given remember kpi values
        | KpiTitle        |
        | Витрати         |
        | Чистий прибуток |
    And I open the add operation modal
    When fill form
        | ElementLocator        | Action | Value   |
        | #operation-amount     | fill   | 15050   |
        | #operation-category   | select | 1       |
        | #operation-date       | fill   | {today} |
        | #btn-submit-operation | click  |         |
    Then check kpi values changed
        | KpiTitle        | Change    |
        | Витрати         | increased |
        | Чистий прибуток | decreased |

Scenario Outline: Operation type toggle switches active button and hidden value (<Type>)
    Given I open the add operation modal
    When click element "#btn-type-<Type>"
    Then check elements class
        | ElementLocator    | ClassName | HasClass |
        | #btn-type-<Type>  | active    | true     |
        | #btn-type-<Other> | active    | false    |
    And check elements value
        | ElementLocator  | ExpectedValue |
        | #operation-type | <Type>        |

    Examples:
        | Type    | Other   |
        | income  | expense |
        | expense | income  |

# ---------- Модалка "Нова організація" ----------

Scenario: Open and close the add organization modal
    When click elements
        | ElementLocator               |
        | #org-select-toggle           |
        | #open-organization-modal-btn |
    Then check elements visibility
        | ElementLocator         | Visible |
        | #form-add-organization | true    |
    When click elements
        | ElementLocator              |
        | #js-close-organization-modal |
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false    |

Scenario: Add organization form requires name
    Given I open the add organization modal
    When fill form
        | ElementLocator           | Action | Value |
        | #organization-name       | fill   |       |
        | #btn-submit-organization | click  |       |
    Then check elements visibility
        | ElementLocator                                                              | Visible |
        | #form-add-organization .form-group:has(#organization-name) .form-error-text | true    |

Scenario: Successfully create a new organization and it becomes active
    Given I open the add organization modal
    When fill form
        | ElementLocator           | Action | Value    |
        | #organization-name       | fill   | {unique} |
        | #btn-submit-organization | click  |          |
    Then current url contains "organizationId="
    And check elements existence
        | ElementLocator      | ElementTagName | AttributeName | ElementValue |
        | #org-select-current | span           |               | {unique}     |
    When click elements
        | ElementLocator     |
        | #org-select-toggle |
    Then the organization dropdown contains
        | OrganizationName | IsActive |
        | {unique}         | true     |

# ---------- Фільтри списку операцій ----------
# Перевірка "visible >0" вимагає, щоб у тестового юзера були операції обох типів

Scenario Outline: Filter operations list by <Filter>
    When click element "#operations-filter .filter-toggle:text-is('<Filter>')"
    Then check elements count
        | ElementLocator                                                   | ExpectedCount |
        | #operations-list .ledger-item[data-type='<HiddenType>']:visible  | 0             |
        | #operations-list .ledger-item[data-type='<VisibleType>']:visible | >0            |

    Examples:
        | Filter  | VisibleType | HiddenType |
        | Дохід   | income      | expense    |
        | Витрати | expense     | income     |
