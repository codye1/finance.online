Feature: Settings page

Background:
    Given I am logged in and on the settings page

# ---------- Сторінка ----------

Scenario: Settings page is displayed
    Then check elements existence
        | ElementLocator                   | ElementTagName | AttributeName | ElementValue |
        | .settings-header h1              | h1             |               | Налаштування організації |
        | .settings-organization           | div            |               | |
        | .settings-organization-body h2   | h2             |               | |
        | .settings-members-list           | div            |               | |
        | #open-invite-member-modal-btn    | button         |               | |
        | #open-delete-organization-dialog | button         |               | |

# Тестовий користувач - власник активної організації.

Scenario: Current user is shown in the members list as owner
    Then check elements existence
        | ElementLocator                                                  | ElementTagName | AttributeName | ElementValue |
        | .settings-member:has-text('newuser@example.com') .settings-pill | span           |               | Ви |
        | .settings-member:has-text('newuser@example.com') .settings-role | span           |               | Owner |
    And check elements class
        | ElementLocator                                                  | ClassName | HasClass |
        | .settings-member:has-text('newuser@example.com') .settings-role | is-owner  | true |

# ---------- Запрошення учасника: модалка ----------

Scenario: Open and close the invite member modal
    When click elements
        | ElementLocator |
        | #open-invite-member-modal-btn |
    Then check elements visibility
        | ElementLocator      | Visible |
        | #form-invite-member | true |
    When click elements
        | ElementLocator |
        | #js-close-invite-member-modal |
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #app-modal     | is-active | false |

Scenario: Invite member form defaults
    Given I open the invite member modal
    Then check elements value
        | ElementLocator | ExpectedValue |
        | #member-email  | |
        | #member-role   | accountant |
    And check elements count
        | ElementLocator      | ExpectedCount |
        | #member-role option | 3 |

# ---------- Запрошення учасника: валідація ----------
# Порожній email jQuery Validate перевіряє лише при сабміті, некоректний - вже при blur.
# У обох випадках помилка видима, submit disabled, модалка лишається відкритою.

Scenario Outline: Invite member form validation - <Case>
    Given I open the invite member modal
    When fill form
        | ElementLocator   | Action          | Value |
        | #member-email    | fill            | <Email> |
        | <TriggerLocator> | <TriggerAction> | |
    Then check elements existence
        | ElementLocator                                                          | ElementTagName | AttributeName | ElementValue |
        | #form-invite-member .form-group:has(#member-email) span.form-error-text | span           |               | <Error> |
    And check elements state
        | ElementLocator            | Enabled |
        | #btn-submit-invite-member | false |
    And check elements visibility
        | ElementLocator      | Visible |
        | #form-invite-member | true |

    Examples:
        | Case       | Email        | TriggerLocator            | TriggerAction | Error |
        | empty      |              | #btn-submit-invite-member | click         | Вкажіть email користувача |
        | no at sign | not-an-email | #member-email             | blur          | Введіть коректний email |
        | no domain  | user@        | #member-email             | blur          | Введіть коректний email |

Scenario: Valid email passes validation
    Given I open the invite member modal
    When fill form
        | ElementLocator | Action | Value |
        | #member-email  | fill   | user@example.com |
        | #member-email  | blur   | |
    Then check elements state
        | ElementLocator            | Enabled |
        | #btn-submit-invite-member | true |
    And check elements visibility
        | ElementLocator                                                          | Visible |
        | #form-invite-member .form-group:has(#member-email) span.form-error-text | false |

# ---------- Запрошення учасника: створення ----------
# member@example.com - вже зареєстрований користувач. Після успіху сторінка
# робить window.location.reload(), тож перевіряємо список після перезавантаження.

Scenario: Successfully invite an existing user
    Given I open the invite member modal
    When fill form
        | ElementLocator            | Action | Value |
        | #member-email             | fill   | member@example.com |
        | #btn-submit-invite-member | click  | |
    Then check elements visibility
        | ElementLocator                                  | Visible |
        | .settings-member:has-text('member@example.com') | true |
    And check elements existence
        | ElementLocator                                                 | ElementTagName | AttributeName | ElementValue |
        | .settings-member:has-text('member@example.com') .settings-role | span           |               | Member |

# ---------- Видалення учасника ----------
# colleague@example.com засіяний у FakeApiState як учасник (accountant) засіяної організації.
# Reset() відновлює його перед кожним сценарієм, тож видалення в одному сценарії
# не впливає на інші. Текст confirm() перевіряємо без email: крок
# "a dialog is shown containing" не підставляє плейсхолдери.

Scenario: Members list shows the seeded colleague with the member role
    Then check elements existence
        | ElementLocator                                                                 | ElementTagName | AttributeName | ElementValue |
        | .settings-member:has-text('colleague@example.com') .settings-role              | span           |               | Member |
        | .settings-member:has-text('colleague@example.com') .settings-member-remove-btn | button         |               | |
    And check elements class
        | ElementLocator                                                    | ClassName | HasClass |
        | .settings-member:has-text('colleague@example.com') .settings-role | is-owner  | false |

Scenario: Owner has no remove button next to himself
    Then check elements visibility
        | ElementLocator                                   | Visible |
        | .settings-member:has-text('newuser@example.com') | true |
    And check elements count
        | ElementLocator                                                               | ExpectedCount |
        | .settings-member:has-text('newuser@example.com') .settings-member-remove-btn | 0 |

Scenario Outline: Remove a member and <Action> the confirmation
    Given the browser <Action> dialogs
    When click element ".settings-member:has-text('colleague@example.com') .settings-member-remove-btn"
    Then a dialog is shown containing "Видалити учасника"
    And check elements count
        | ElementLocator                                     | ExpectedCount |
        | .settings-member:has-text('colleague@example.com') | <Count> |
    And check elements visibility
        | ElementLocator                                   | Visible |
        | .settings-member:has-text('newuser@example.com') | true |

    Examples:
        | Action    | Count |
        | accepts   | 0 |
        | dismisses | 1 |

Scenario: A removed member can be invited again
    Given the browser accepts dialogs
    And click element ".settings-member:has-text('colleague@example.com') .settings-member-remove-btn"
    And check elements count
        | ElementLocator                                     | ExpectedCount |
        | .settings-member:has-text('colleague@example.com') | 0 |
    And I open the invite member modal
    And fill form
        | ElementLocator            | Action | Value |
        | #member-email             | fill   | colleague@example.com |
        | #btn-submit-invite-member | click  | |
    Then check elements visibility
        | ElementLocator                                     | Visible |
        | .settings-member:has-text('colleague@example.com') | true |

# ---------- Видалення організації: діалог ----------
# Реальне видалення тестується лише на організації, яку сценарій сам створює
# (див. останній сценарій), щоб не зачепити засіяні дані інших feature.

Scenario: Open the delete organization dialog
    When click elements
        | ElementLocator |
        | #open-delete-organization-dialog |
    Then check elements class
        | ElementLocator              | ClassName | HasClass |
        | #delete-organization-dialog | is-active | true |
    And check elements attribute contains
        | ElementLocator              | AttributeName | ExpectedValue |
        | #delete-organization-dialog | aria-hidden   | false |
    And check elements existence
        | ElementLocator             | ElementTagName | AttributeName | ElementValue |
        | #delete-organization-title | h2             |               | Видалити організацію? |
    And the delete organization dialog mentions the organization name

Scenario: Cancel closes the delete organization dialog
    Given I open the delete organization dialog
    When click elements
        | ElementLocator |
        | #cancel-delete-organization |
    Then check elements class
        | ElementLocator              | ClassName | HasClass |
        | #delete-organization-dialog | is-active | false |
    And check elements attribute contains
        | ElementLocator              | AttributeName | ExpectedValue |
        | #delete-organization-dialog | aria-hidden   | true |
    And current url contains "/settings"
    And check elements visibility
        | ElementLocator         | Visible |
        | .settings-organization | true |

Scenario: Escape closes the delete organization dialog
    Given I open the delete organization dialog
    When I press the Escape key
    Then check elements class
        | ElementLocator              | ClassName | HasClass |
        | #delete-organization-dialog | is-active | false |

Scenario: Clicking the overlay closes the delete organization dialog
    Given I open the delete organization dialog
    When I click outside the delete organization dialog
    Then check elements class
        | ElementLocator              | ClassName | HasClass |
        | #delete-organization-dialog | is-active | false |

Scenario: Clicking inside the delete organization dialog keeps it open
    Given I open the delete organization dialog
    When click element "#delete-organization-title"
    Then check elements class
        | ElementLocator              | ClassName | HasClass |
        | #delete-organization-dialog | is-active | true |

# ---------- Видалення організації: повний цикл ----------
# Спочатку створюємо власну організацію ({unique}), одразу її видаляємо
# і перевіряємо, що вона зникла з dropdown на Home. Засіяні організації не чіпаємо.

Scenario: Create an organization and delete it - it disappears from the home dropdown
    When click elements
        | ElementLocator                  |
        | #open-create-organization-modal |
    And fill form
        | ElementLocator           | Action | Value    |
        | #organization-name       | fill   | {unique} |
        | #btn-submit-organization | click  |          |
    Then current url contains "organizationId="
    # Запобіжник: без цієї перевірки тест міг би видалити не ту (засіяну) організацію.
    And check elements existence
        | ElementLocator                 | ElementTagName | AttributeName | ElementValue |
        | .settings-organization-body h2 | h2             |               | {unique}     |
    When click elements
        | ElementLocator                                  |
        | #open-delete-organization-dialog                |
        | #delete-organization-form button[type='submit'] |
    Then the current path is "/"
    And check elements count
        | ElementLocator                                             | ExpectedCount |
        | #org-select-current:has-text('{unique}')                   | 0             |
        | #org-select-dropdown .org-select-item:has-text('{unique}') | 0             |
    When click elements
        | ElementLocator     |
        | #org-select-toggle |
    Then check elements visibility
        | ElementLocator                                             | Visible |
        | #org-select-dropdown                                       | true    |
    And check elements count
        | ElementLocator                                             | ExpectedCount |
        | #org-select-dropdown .org-select-item:has-text('{unique}') | 0             |
