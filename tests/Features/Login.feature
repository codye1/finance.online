Feature: User authentication

Background:
    Given I am on the auth page

Scenario: Login panel is shown by default
    Then the login panel is visible
    And the register panel is hidden

Scenario: Switch from login to register panel
    When I click the toggle link
    Then the register panel is visible
    And the login panel is hidden

Scenario: Switch back from register to login panel
    When I click the toggle link
    And I click the toggle link
    Then the login panel is visible
    And the register panel is hidden

Scenario: Login with invalid credentials shows an error
    When I log in with email "wrong@example.com" and password "wrongpass"
    Then I see a login error message

Scenario: Login form requires email
    When I submit the login form without an email
    Then the browser shows a validation message for the email field

Scenario: Login form requires password
    When I submit the login form without a password
    Then the browser shows a validation message for the password field

Scenario: Successful registration
    When I switch to the register panel
    And I register with a new unique email and password "Password123!"
    Then I see the registration success alert
    And the login panel is visible

Scenario: Successful registration followed by login
    Given I register with a new unique email and password "Password123!" and accept the alert
    When I log in with the registered email and password "Password123!"
    Then I am redirected to the home page

Scenario: Registration with an already used email shows an error
    Given a user is already registered
    When I switch to the register panel
    And I register with the existing email and password "Password123!"
    Then I see a register error message