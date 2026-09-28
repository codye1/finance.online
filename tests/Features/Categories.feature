Feature: Categories page

Background:
    Given I am logged in and on the categories page

# ---------- Сторінка ----------

Scenario: Categories page is displayed
    Then check elements existence
        | ElementLocator                | ElementTagName | AttributeName | ElementValue |
        | .categories-header h1         | h1             |               | Категорії    |
        | #categories-list              | div            |               |              |
        | #open-add-category-modal-btn  | button         |               |              |

# ---------- Модалка ----------

Scenario: Open and close the add category modal
    When click elements
        | ElementLocator               |
        | #open-add-category-modal-btn |
    Then check elements visibility
        | ElementLocator     | Visible |
        | #form-add-category | true    |
    When click elements
        | ElementLocator            |
        | #js-close-category-modal  |
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false    |

# ---------- Валідація ----------
# Порожнє ім'я jQuery Validate перевіряє лише при сабміті, задовге ім'я - при blur.
# У обох випадках помилка видима, submit disabled, модалка лишається відкритою.

Scenario Outline: Add category form validation - <Case>
    Given I open the add category modal
    When fill form
        | ElementLocator   | Action          | Value  |
        | #category-name   | fill            | <Name> |
        | <TriggerLocator> | <TriggerAction> |        |
    Then check elements existence
        | ElementLocator                                                             | ElementTagName | AttributeName | ElementValue |
        | #form-add-category .form-group:has(#category-name) span.form-error-text    | span           |               | <Error>      |
    And check elements state
        | ElementLocator       | Enabled |
        | #btn-submit-category | false   |
    And check elements visibility
        | ElementLocator     | Visible |
        | #form-add-category | true    |

    Examples:
        | Case      | Name       | TriggerLocator       | TriggerAction | Error                                |
        | empty     |            | #btn-submit-category | click         | Вкажіть назву категорії              |
        | 101 chars | {chars:101} | #category-name      | blur          | Назва має бути не довше 100 символів |

Scenario: Category name of exactly 100 characters is valid
    Given I open the add category modal
    When fill form
        | ElementLocator | Action | Value       |
        | #category-name | fill   | {chars:100} |
        | #category-name | blur   |             |
    Then check elements state
        | ElementLocator       | Enabled |
        | #btn-submit-category | true    |
    And check elements visibility
        | ElementLocator                                                          | Visible |
        | #form-add-category .form-group:has(#category-name) span.form-error-text | false   |

# ---------- Колір ----------

Scenario Outline: Select a category color (<Color>)
    Given I open the add category modal
    When click element ".color-swatch-btn[data-color='<Color>']"
    Then check elements class
        | ElementLocator                          | ClassName | HasClass |
        | .color-swatch-btn[data-color='<Color>'] | active    | true     |
        | .color-swatch-btn[data-color='#2563EB'] | active    | false    |
    And check elements value
        | ElementLocator  | ExpectedValue |
        | #category-color | <Color>       |

    Examples:
        | Color   |
        | #10B981 |
        | #EF4444 |
        | #F59E0B |
        | #8B5CF6 |
        | #EC4899 |
        | #06B6D4 |
        | #84CC16 |

# ---------- Створення ----------

Scenario Outline: Successfully add a new category with color <Color>
    Given I open the add category modal
    When fill form
        | ElementLocator                          | Action | Value    |
        | #category-name                          | fill   | {unique} |
        | .color-swatch-btn[data-color='<Color>'] | click  |          |
        | #btn-submit-category                    | click  |          |
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false    |
    And check elements visibility
        | ElementLocator                                         | Visible |
        | #categories-list .categories-item:has-text('{unique}') | true    |
    And check elements attribute contains
        | ElementLocator                                                                    | AttributeName | ExpectedValue |
        | #categories-list .categories-item:has-text('{unique}') .category-color-dot        | style         | <Color>       |

    Examples:
        | Color   |
        | #2563EB |
        | #EF4444 |

# ---------- Видалення ----------
# Видаляється ТІЛЬКИ категорія, створена самим сценарієм ({unique}),
# щоб не зачепити засіяні категорії для тестів Operations.

Scenario Outline: Delete a category and <Action> the confirmation
    Given I open the add category modal
    And fill form
        | ElementLocator       | Action | Value    |
        | #category-name       | fill   | {unique} |
        | #btn-submit-category | click  |          |
    And check elements visibility
        | ElementLocator                                         | Visible |
        | #categories-list .categories-item:has-text('{unique}') | true    |
    And check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false    |
    And the browser <Action> dialogs
    When click element "#categories-list .categories-item:has-text('{unique}') .category-delete-btn"
    Then a dialog is shown containing "Видалити цю категорію?"
    And check elements count
        | ElementLocator                                         | ExpectedCount |
        | #categories-list .categories-item:has-text('{unique}') | <Count>       |

    Examples:
        | Action    | Count |
        | accepts   | 0     |
        | dismisses | 1     |
