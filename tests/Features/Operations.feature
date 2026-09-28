Feature: Operations page

Background:
    Given I am logged in and on the operations page

# ---------- Сторінка ----------

Scenario: Operations page is displayed
    Then check elements existence
        | ElementLocator            | ElementTagName | AttributeName | ElementValue |
        | .op-header h1             | h1             |               | Усі операції |
        | #open-operation-modal-btn | button         |               | |
        | #op-summary-income        | p              |               | |
        | #op-summary-expense       | p              |               | |
        | #op-summary-net           | p              |               | |
        | #op-search-input          | input          |               | |
        | #op-type-filter           | select         |               | |
        | #op-category-filter       | select         |               | |
        | #operations-list          | div            |               | |

# ---------- Модалка ----------

Scenario: Open and close the add operation modal
    When click elements
        | ElementLocator |
        | #open-operation-modal-btn |
    Then check elements visibility
        | ElementLocator      | Visible |
        | #form-add-operation | true |
    When click elements
        | ElementLocator |
        | #js-close-operation-modal |
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false |

Scenario: Add operation modal has expense selected by default
    Given I open the add operation modal
    Then check elements class
        | ElementLocator    | ClassName | HasClass |
        | #btn-type-expense | active    | true |
        | #btn-type-income  | active    | false |
    And check elements value
        | ElementLocator      | ExpectedValue |
        | #operation-type     | expense |
        | #operation-category | |

# ---------- Тип операції ----------

Scenario Outline: Switch operation type to <Type>
    Given I open the add operation modal
    When click element "#btn-type-<Type>"
    Then check elements class
        | ElementLocator    | ClassName | HasClass |
        | #btn-type-<Type>  | active    | true |
        | #btn-type-<Other> | active    | false |
    And check elements value
        | ElementLocator  | ExpectedValue |
        | #operation-type | <Type> |

    Examples:
        | Type    | Other |
        | income  | expense |
        | expense | income |

# ---------- Валідація ----------
# Порожню суму / категорію jQuery Validate перевіряє лише при сабміті,
# а некоректну суму (0, від'ємну) - вже при blur.
# У всіх випадках помилка видима, submit disabled, модалка лишається відкритою.
# Індекс 0 у #operation-category - плейсхолдер "Оберіть категорію", 1 - перша реальна категорія.

Scenario Outline: Add operation form validation - <Case>
    Given I open the add operation modal
    When fill form
        | ElementLocator      | Action          | Value |
        | #operation-amount   | fill            | <Amount> |
        | #operation-category | select          | <Category> |
        | <TriggerLocator>    | <TriggerAction> | |
    Then check elements existence
        | ElementLocator                                                       | ElementTagName | AttributeName | ElementValue |
        | #form-add-operation .op-form-group:has(<Field>) span.form-error-text | span           |               | <Error> |
    And check elements state
        | ElementLocator        | Enabled |
        | #btn-submit-operation | false |
    And check elements visibility
        | ElementLocator      | Visible |
        | #form-add-operation | true |

    Examples:
        | Case        | Amount | Category | Field               | TriggerLocator        | TriggerAction | Error |
        | no amount   |        | 1        | #operation-amount   | #btn-submit-operation | click         | Вкажіть суму |
        | zero amount | 0      | 1        | #operation-amount   | #operation-amount     | blur          | Сума повинна бути більше 0 |
        | negative    | -5     | 1        | #operation-amount   | #operation-amount     | blur          | Сума повинна бути більше 0 |
        | no category | 100    | 0        | #operation-category | #btn-submit-operation | click         | Оберіть категорію |

Scenario: Minimum amount of 0.01 is valid
    Given I open the add operation modal
    When fill form
        | ElementLocator    | Action | Value |
        | #operation-amount | fill   | 0.01 |
        | #operation-amount | blur   | |
    Then check elements state
        | ElementLocator        | Enabled |
        | #btn-submit-operation | true |
    And check elements visibility
        | ElementLocator                                                                 | Visible |
        | #form-add-operation .op-form-group:has(#operation-amount) span.form-error-text | false |

# ---------- Створення ----------
# Опис = {unique}, щоб однозначно знайти саме створену операцію серед засіяних.

Scenario Outline: Successfully add a new <Type> operation
    Given I open the add operation modal
    When fill form
        | ElementLocator         | Action | Value |
        | #btn-type-<Type>       | click  | |
        | #operation-amount      | fill   | 150.5 |
        | #operation-category    | select | 1 |
        | #operation-description | fill   | {unique} |
        | #btn-submit-operation  | click  | |
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false |
    And check elements visibility
        | ElementLocator                                               | Visible |
        | #operations-list > .op-item:first-child:has-text('{unique}') | true |
    And check elements attribute contains
        | ElementLocator                                 | AttributeName | ExpectedValue |
        | #operations-list .op-item:has-text('{unique}') | data-type     | <Type> |
        | #operations-list .op-item:has-text('{unique}') | data-amount   | 150.5 |

    Examples:
        | Type |
        | expense |
        | income |

# ---------- Пошук ----------
# Створюємо операцію з {unique}, щоб пошук не залежав від засіяних даних.

Scenario: Search shows only matching operations
    Given I open the add operation modal
    And fill form
        | ElementLocator         | Action | Value |
        | #operation-amount      | fill   | 100 |
        | #operation-category    | select | 1 |
        | #operation-description | fill   | {unique} |
        | #btn-submit-operation  | click  | |
    And check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false |
    When fill form
        | ElementLocator   | Action | Value |
        | #op-search-input | fill   | {unique} |
    Then check elements count
        | ElementLocator                                 | ExpectedCount |
        | #operations-list .op-item:visible              | 1 |
        | #operations-list .op-item:has-text('{unique}') | 1 |

Scenario: Search without matches shows the empty state
    When fill form
        | ElementLocator   | Action | Value |
        | #op-search-input | fill   | no-such-operation-zzz999 |
    Then check elements count
        | ElementLocator                    | ExpectedCount |
        | #operations-list .op-item:visible | 0 |
    And check elements visibility
        | ElementLocator  | Visible |
        | #op-empty-state | true |

# ---------- Фільтр за типом ----------

Scenario Outline: Filter operations by type "<Label>"
    Given I open the add operation modal
    And fill form
        | ElementLocator         | Action | Value |
        | #btn-type-<Type>       | click  | |
        | #operation-amount      | fill   | 100 |
        | #operation-category    | select | 1 |
        | #operation-description | fill   | {unique} |
        | #btn-submit-operation  | click  | |
    And check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false |
    When fill form
        | ElementLocator  | Action | Value |
        | #op-type-filter | select | <Label> |
    Then check elements visibility
        | ElementLocator                                 | Visible |
        | #operations-list .op-item:has-text('{unique}') | true |
    And check elements count
        | ElementLocator                                            | ExpectedCount |
        | #operations-list .op-item[data-type='<Opposite>']:visible | 0 |

    Examples:
        | Type    | Label   | Opposite |
        | income  | Дохід   | expense |
        | expense | Витрати | income |

# ---------- Фільтр за категорією ----------
# Операція створюється в категорії №2, фільтр по категорії №1 має її сховати.
# (Індекси у #op-category-filter: 0 - "Усі категорії", далі ті самі категорії,
# що й у формі. Потрібно щонайменше 2 категорії.)

Scenario: Filter operations by category
    Given I open the add operation modal
    And fill form
        | ElementLocator         | Action | Value |
        | #operation-amount      | fill   | 100 |
        | #operation-category    | select | 2 |
        | #operation-description | fill   | {unique} |
        | #btn-submit-operation  | click  | |
    And check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false |
    When fill form
        | ElementLocator      | Action | Value |
        | #op-category-filter | select | 1 |
    Then check elements count
        | ElementLocator                                         | ExpectedCount |
        | #operations-list .op-item:has-text('{unique}'):visible | 0 |
    When fill form
        | ElementLocator      | Action | Value |
        | #op-category-filter | select | 2 |
    Then check elements visibility
        | ElementLocator                                 | Visible |
        | #operations-list .op-item:has-text('{unique}') | true |
    When fill form
        | ElementLocator      | Action | Value |
        | #op-category-filter | select | 0 |
    Then check elements visibility
        | ElementLocator                                 | Visible |
        | #operations-list .op-item:has-text('{unique}') | true |

# ---------- Видалення ----------
# Видаляється ТІЛЬКИ операція, створена самим сценарієм ({unique}),
# щоб не зачепити засіяні дані.

Scenario Outline: Delete an operation and <Action> the confirmation
    Given I open the add operation modal
    And fill form
        | ElementLocator         | Action | Value |
        | #operation-amount      | fill   | 100 |
        | #operation-category    | select | 1 |
        | #operation-description | fill   | {unique} |
        | #btn-submit-operation  | click  | |
    And check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false |
    And check elements visibility
        | ElementLocator                                 | Visible |
        | #operations-list .op-item:has-text('{unique}') | true |
    And the browser <Action> dialogs
    When click element "#operations-list .op-item:has-text('{unique}') .op-delete-btn"
    Then a dialog is shown containing "Видалити цю операцію?"
    And check elements count
        | ElementLocator                                 | ExpectedCount |
        | #operations-list .op-item:has-text('{unique}') | <Count> |

    Examples:
        | Action    | Count |
        | accepts   | 0 |
        | dismisses | 1 |
