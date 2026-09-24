Feature: Operations page

Background:
    Given I am logged in and on the operations page

Scenario: Summary cards are displayed
    Then I see the operations summary cards

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
    When I fill in the operation amount "150" and select the first category
    And I submit the operation form
    Then the new operation appears at the top of the operations list

Scenario: Switch operation type to income
    Given I open the add operation modal
    When I click the income type toggle
    Then the income type toggle is active
    And the expense type toggle is not active

Scenario: Search filters the operations list
    When I search operations for "Salary"
    Then only operations matching "Salary" are visible in the list

Scenario: Filter operations list by type income
    When I filter operations by type "Дохід"
    Then only income operations are visible in the list

Scenario: Filter operations list by type expense
    When I filter operations by type "Витрати"
    Then only expense operations are visible in the list

Scenario: Filter operations list by category
    Given I note the first category name
    When I filter operations by that category
    Then only operations of that category are visible in the list

Scenario: Delete an operation
    Given there is at least one operation in the list
    When I delete the first operation and confirm
    Then the operation is removed from the list
