# Introduce Assertion

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Conditional Expressions
Introduce Assertion
Assertion testing here refers to use of assert() calls.
Problem
For a portion of code to work correctly, certain conditions or values must be true.
Solution
Replace these assumptions with specific assertion checks.
double getExpenseLimit() {
  // Should have either expense limit or
  // a primary project.
  return (expenseLimit != NULL_EXPENSE) ?
    expenseLimit :
    primaryProject.getMemberExpenseLimit();
}
double getExpenseLimit() {
  Assert.isTrue(expenseLimit != NULL_EXPENSE || primaryProject != null);
  return (expenseLimit != NULL_EXPENSE) ?
    expenseLimit:
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Introduce Assertion
https://sourcemaking.com/refactoring/introduce-assertion 1/3

Why Refactor
Say that a portion of code assumes something about, for example, the current condition of an
object or value of a parameter or local variable. Usually this assumption will always hold true
except in the event of an error.
Make these assumptions obvious by adding corresponding assertions. As with type hinting in
method parameters, these assertions can act as live documentation for your code.
As a guideline to see where your code needs assertions, check for comments that describe the
conditions under which a particular method will work.
Benefits
If an assumption is not true and the code therefore gives the wrong result, it is better to stop
execution before this causes fatal consequences and data corruption. This also means that you
neglected to write a necessary test when devising ways to perform testing of the program.
Drawbacks
Sometimes an exception is more appropriate than a simple assertion. You can select the
necessary class of the exception and let the remaining code handle it correctly.
When is an exception better than a simple assertion? If the exception can be caused by actions
of the user or system and you can handle the exception. On the other hand, ordinary unnamed
and unhandled exceptions are basically equivalent to simple assertions – you do not handle
them and they are caused exclusively as the result of a program bug that never should have
occurred.
How to Refactor
    primaryProject.getMemberExpenseLimit();
}
23.08.2026, 11:11 Introduce Assertion
https://sourcemaking.com/refactoring/introduce-assertion 2/3

When you see that a condition is assumed, add an assertion for this condition in order to make
sure.
Adding the assertion should not change the program’s behavior.
Do not overdo it with use of assertions for everything in your code. Check for only the conditions
that are necessary for correct functioning of the code. If your code is working normally even when a
particular assertion is false, you can safely remove the assertion.
RETURN READ NEXT
Simplifying Method Calls    Introduce Null Object
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:11 Introduce Assertion
https://sourcemaking.com/refactoring/introduce-assertion 3/3
