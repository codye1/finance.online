Feature: User authentication

Background:
    Given I am on the auth page

# ---------- Перемикання панелей ----------

Scenario: Login panel is shown by default
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #loginPanel    | hidden    | false    |
        | #registerPanel | hidden    | true     |
    And check elements visibility
        | ElementLocator | Visible |
        | #loginPanel    | true    |

Scenario: Toggle link switches between login and register panels
    When click element "#loginPanel .toggle-auth-panels"
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #loginPanel    | hidden    | true     |
        | #registerPanel | hidden    | false    |
    And check elements visibility
        | ElementLocator | Visible |
        | #registerPanel | true    |
    When click element "#registerPanel .toggle-auth-panels"
    Then check elements class
        | ElementLocator | ClassName | HasClass |
        | #loginPanel    | hidden    | false    |
        | #registerPanel | hidden    | true     |
    And check elements visibility
        | ElementLocator | Visible |
        | #loginPanel    | true    |

# ---------- Логін ----------

Scenario: Login with invalid credentials shows an error
    When fill form
        | ElementLocator                 | Action | Value             |
        | #loginEmail                    | fill   | wrong@example.com |
        | #loginPassword                 | fill   | wrongpass         |
        | #loginForm button[type=submit] | click  |                   |
    Then check elements visibility
        | ElementLocator            | Visible |
        | #loginForm .server-error  | true    |

# Помилки показує jQuery Validate (label.error), а не браузер
Scenario Outline: Login form validation - <Case>
    When fill form
        | ElementLocator                 | Action | Value      |
        | #loginEmail                    | fill   | <Email>    |
        | #loginPassword                 | fill   | <Password> |
        | #loginForm button[type=submit] | click  |            |
    Then check elements existence
        | ElementLocator             | ElementTagName | AttributeName | ElementValue |
        | label.error[for='<Field>'] | label          |               | <Error>      |

    Examples:
        | Case             | Field         | Email            | Password     | Error                      |
        | empty email      | loginEmail    |                  | Password123! | Будь ласка, введіть пошту. |
        | invalid email    | loginEmail    | not-an-email     | Password123! | Введіть коректну адресу.   |
        | empty password   | loginPassword | user@example.com |              | Будь ласка, введіть пароль. |

# ---------- Реєстрація ----------

Scenario Outline: Register form validation - <Case>
    When click element "#loginPanel .toggle-auth-panels"
    And fill form
        | ElementLocator                    | Action | Value      |
        | #registerEmail                    | fill   | <Email>    |
        | #registerPassword                 | fill   | <Password> |
        | #registerForm button[type=submit] | click  |            |
    Then check elements existence
        | ElementLocator             | ElementTagName | AttributeName | ElementValue |
        | label.error[for='<Field>'] | label          |               | <Error>      |

    Examples:
        | Case             | Field            | Email            | Password     | Error                                                                             |
        | empty email      | registerEmail    |                  | Password123! | Будь ласка, введіть пошту.                                                        |
        | invalid email    | registerEmail    | not-an-email     | Password123! | Введіть коректну адресу електронної пошти.                                        |
        | empty password   | registerPassword | user@example.com |              | Будь ласка, введіть пароль.                                                       |
        | too short        | registerPassword | user@example.com | Ab1          | Пароль має містити щонайменше 6 символів.                                         |
        | no uppercase     | registerPassword | user@example.com | password123  | Пароль повинен містити принаймні одну велику літеру, одну малу літеру та одну цифру. |
        | no lowercase     | registerPassword | user@example.com | PASSWORD123  | Пароль повинен містити принаймні одну велику літеру, одну малу літеру та одну цифру. |
        | no digit         | registerPassword | user@example.com | Password     | Пароль повинен містити принаймні одну велику літеру, одну малу літеру та одну цифру. |

Scenario: Successful registration
    Given the browser accepts dialogs
    When click element "#loginPanel .toggle-auth-panels"
    And fill form
        | ElementLocator                    | Action | Value          |
        | #registerEmail                    | fill   | {uniqueEmail}  |
        | #registerPassword                 | fill   | Password123!   |
        | #registerForm button[type=submit] | click  |                |
    Then a dialog is shown containing "успішна"
    And check elements class
        | ElementLocator | ClassName | HasClass |
        | #loginPanel    | hidden    | false    |
        | #registerPanel | hidden    | true     |

Scenario: Successful registration followed by login
    Given the browser accepts dialogs
    And click element "#loginPanel .toggle-auth-panels"
    And fill form
        | ElementLocator                    | Action | Value          |
        | #registerEmail                    | fill   | {uniqueEmail}  |
        | #registerPassword                 | fill   | Password123!   |
        | #registerForm button[type=submit] | click  |                |
    And a dialog is shown containing "успішна"
    When fill form
        | ElementLocator                 | Action | Value          |
        | #loginEmail                    | fill   | {uniqueEmail}  |
        | #loginPassword                 | fill   | Password123!   |
        | #loginForm button[type=submit] | click  |                |
    Then the current path is "/"

Scenario: Registration with an already used email shows an error
    Given a user is already registered
    And the browser accepts dialogs
    When click element "#loginPanel .toggle-auth-panels"
    And fill form
        | ElementLocator                    | Action | Value          |
        | #registerEmail                    | fill   | {uniqueEmail}  |
        | #registerPassword                 | fill   | Password123!   |
        | #registerForm button[type=submit] | click  |                |
    Then no dialog is shown
    And check elements existence
        | ElementLocator                    | ElementTagName | AttributeName | ElementValue |
        | label.error[for='registerEmail']  | label          |               |              |
