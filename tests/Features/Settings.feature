Feature: Settings page

Background:
    Given I am logged in and on the settings page

Scenario: Settings page is displayed
    Then I see the settings page header
    And I see the organization card
    And I see the members section

Scenario: Current user is shown in the members list as owner
    Then the members list contains the current user marked as Ви
    And the current user has the "Owner" role

# ---------- Invite member ----------

Scenario: Open and close the invite member modal
    When I click the invite member button
    Then the invite member modal is visible
    When I close the invite member modal
    Then the invite member modal is hidden

Scenario: Invite member role defaults to accountant
    Given I open the invite member modal
    Then the invite member role is "accountant"

Scenario: Invite member form requires an email
    Given I open the invite member modal
    When I submit the invite member form without an email
    Then I see a validation error for the invite email field

Scenario: Invite member form rejects an invalid email
    Given I open the invite member modal
    When I fill in the invite email "not-an-email"
    And I submit the invite member form
    Then I see a validation error for the invite email field
    And the invite member modal is visible

Scenario: Invite submit button is disabled while the form is invalid
    Given I open the invite member modal
    When I submit the invite member form without an email
    Then the invite member submit button is disabled

Scenario: Successfully invite an existing user
    Given I open the invite member modal
    When I fill in the email of an existing user
    And I submit the invite member form
    Then the invited user appears in the members list

# ---------- Delete organization dialog ----------
# Actually deleting the organization is intentionally not tested:
# it would destroy the seeded data other features depend on.

Scenario: Open the delete organization dialog
    When I click the delete organization button
    Then the delete organization dialog is visible
    And the delete organization dialog mentions the organization name

Scenario: Cancel closes the delete organization dialog
    Given I open the delete organization dialog
    When I click cancel in the delete organization dialog
    Then the delete organization dialog is hidden
    And I am still on the settings page

Scenario: Escape closes the delete organization dialog
    Given I open the delete organization dialog
    When I press the Escape key
    Then the delete organization dialog is hidden

Scenario: Clicking the overlay closes the delete organization dialog
    Given I open the delete organization dialog
    When I click outside the delete organization dialog
    Then the delete organization dialog is hidden
