# Simplifying Conditional Expressions

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques
Simplifying ConditionalExpressions
Conditionals tend to get more and more complicated in their logic over time, and there are yet
more techniques to combat this as well.
Problem: You have a complex conditional (if-then/else or switch).
Solution: Decompose the complicated parts of the conditional into separate methods: the
condition, then and else.
Problem: You have multiple conditionals that lead to the same result or action.
Solution: Consolidate all these conditionals in a single expression.
Problem: Identical code can be found in all branches of a conditional.
Solution: Move the code outside of the conditional.
Problem: You have a boolean variable that acts as a control flag for multiple boolean
expressions.
Solution: Instead of the variable, use break, continue and return.
§ Decompose Conditional
§ Consolidate Conditional Expression
§ Consolidate Duplicate Conditional Fragments
§ Remove Control Flag
§ Replace Nested Conditional with Guard Clauses

 
23.08.2026, 11:11 Simplifying Conditional Expressions
https://sourcemaking.com/refactoring/simplifying-conditional-expressions 1/3

Problem: You have a group of nested conditionals and it is hard to determine the normal flow of
code execution.
Solution: Isolate all special checks and edge cases into separate clauses and place them before
the main checks. Ideally, you should have a “flat” list of conditionals, one after the other.
Problem: You have a conditional that performs various actions depending on object type or
properties.
Solution: Create subclasses matching the branches of the conditional. In them, create a shared
method and move code from the corresponding branch of the conditional to it. Then replace the
conditional with the relevant method call. The result is that the proper implementation will be
attained via polymorphism depending on the object class.
Problem: Since some methods return null instead of real objects, you have many checks for
null in your code.
Solution: Instead of null, return a null object that exhibits the default behavior.
Problem: For a portion of code to work correctly, certain conditions or values must be true.
Solution: Replace these assumptions with specific assertion checks.
RETURN READ NEXT
§ Replace Conditional with Polymorphism
§ Introduce Null Object
§ Introduce Assertion
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:11 Simplifying Conditional Expressions
https://sourcemaking.com/refactoring/simplifying-conditional-expressions 2/3

Decompose Conditional    Replace Subclass with Fields
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:11 Simplifying Conditional Expressions
https://sourcemaking.com/refactoring/simplifying-conditional-expressions 3/3
