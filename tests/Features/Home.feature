Feature: Home dashboard

Background:
    Given I am logged in and on the home page

Scenario: KPI cards are displayed
    Then I see 4 KPI cards

Scenario: Organization dropdown opens and closes
    When I click the organization selector
    Then the organization dropdown is visible
    When I click the organization selector
    Then the organization dropdown is hidden

Scenario: Period dropdown opens and closes
    When I click the period selector
    Then the period dropdown is visible
    When I click the period selector
    Then the period dropdown is hidden

Scenario: Open and close the add operation modal
    When I click the add operation button
    Then the add operation modal is visible
    When I close the modal
    Then the add operation modal is hidden

Scenario: Add operation form requires amount
    Given I open the add operation modal
    When I submit the operation form without an amount
    Then I see a validation error for the amount field

Scenario: Add operation form requires category
    Given I open the add operation modal
    When I submit the operation form without a category
    Then I see a validation error for the category field

Scenario: Successfully add a new expense operation
    Given I open the add operation modal
    When I fill in the operation amount "15050" and select the first category
    And I submit the operation form
    Then the new operation appears at the top of the operations list

Scenario: Switch operation type to income
    Given I open the add operation modal
    When I click the income type toggle
    Then the income type toggle is active
    And the expense type toggle is not active

Scenario: Open and close the add organization modal
    When I click the create organization button
    Then the add organization modal is visible
    When I close the modal
    Then the add organization modal is hidden

Scenario: Add organization form requires name
    Given I open the add organization modal
    When I submit the organization form without a name
    Then I see a validation error for the organization name field

Scenario: Successfully create a new organization
    Given I open the add organization modal
    When I fill in a unique organization name and submit
    Then I am redirected to the home page with the new organization active

Scenario: Filter operations list by income
    When I click the operations filter "Дохід"
    Then only income operations are visible in the list

Scenario: Filter operations list by expense
    When I click the operations filter "Витрати"
    Then only expense operations are visible in the list