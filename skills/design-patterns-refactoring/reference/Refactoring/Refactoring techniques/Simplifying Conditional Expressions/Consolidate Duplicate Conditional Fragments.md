# Consolidate Duplicate Conditional Fragments

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Conditional Expressions
Consolidate DuplicateConditional Fragments
Problem
Identical code can be found in all branches of a conditional.
Solution
Move the code outside of the conditional.
if (isSpecialDeal()) {
  total = price * 0.95;
  send();
}
else {
  total = price * 0.98;
  send();
}
if (isSpecialDeal()) {
  total = price * 0.95;
}
else {
  total = price * 0.98;
}
send();
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Consolidate Duplicate Conditional Fragments
https://sourcemaking.com/refactoring/consolidate-duplicate-conditional-fragments 1/2

Why Refactor
Duplicate code is found inside all branches of a conditional, often as the result of evolution of the
code within the conditional branches. Team development can be a contributing factor to this.
Benefits
Code deduplication.
How to Refactor
1. If the duplicated code is at the beginning of the conditional branches, move the code to a place
before the conditional.
2. If the code is executed at the end of the branches, place it after the conditional.
3. If the duplicate code is randomly situated inside the branches, first try to move the code to the
beginning or end of the branch, depending on whether it changes the result of the subsequent
code.
4. If appropriate and the duplicate code is longer than one line, try using Extract Method.
RETURN READ NEXT
Remove Control Flag    Consolidate Conditional Expression
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
23.08.2026, 11:11 Consolidate Duplicate Conditional Fragments
https://sourcemaking.com/refactoring/consolidate-duplicate-conditional-fragments 2/2
