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

# ---------- Видалення організації: діалог ----------
# Саме видалення організації навмисно не тестується:
# воно знищило б засіяні дані, від яких залежать інші feature.

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
