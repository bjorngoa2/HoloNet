# Composing Methods

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques
Composing Methods
Much of refactoring is devoted to correctly composing methods. In most cases, excessively long
methods are the root of all evil. The vagaries of code inside these methods conceal the execution
logic and make the method extremely hard to understand – and even harder to change.
The refactoring techniques in this group streamline methods, remove code duplication, and pave
the way for future improvements.
Problem: You have a code fragment that can be grouped together.
Solution: Move this code to a separate new method (or function) and replace the old code with a
call to the method.
Problem: When a method body is more obvious than the method itself, use this technique.
Solution: Replace calls to the method with the method’s content and delete the method itself.
Problem: You have an expression that is hard to understand.
Solution: Place the result of the expression or its parts in separate variables that are self-
explanatory.
Problem: You have a temporary variable that is assigned the result of a simple expression and
nothing more.
§ Extract Method
§ Inline Method
§ Extract Variable
§ Inline Temp

 
23.08.2026, 11:10 Composing Methods
https://sourcemaking.com/refactoring/composing-methods 1/3

Solution: Replace the references to the variable with the expression itself.
Problem: You place the result of an expression in a local variable for later use in your code.
Solution: Move the entire expression to a separate method and return the result from it. Query
the method instead of using a variable. Incorporate the new method in other methods, if
necessary.
Problem: You have a local variable that is used to store various intermediate values inside a
method (except for cycle variables).
Solution: Use different variables for different values. Each variable should be responsible for
only one particular thing.
Problem: Some value is assigned to a parameter inside method’s body.
Solution: Use a local variable instead of a parameter.
Problem: You have a long method in which the local variables are so intertwined that you
cannot apply Extract Method.
Solution: Transform the method into a separate class so that the local variables become fields of
the class. Then you can split the method into several methods within the same class.
Problem: So you want to replace an existing algorithm with a new one?
Solution: Replace the body of the method that implements the algorithm with a new algorithm.
§ Replace Temp with Query
§ Split Temporary Variable
§ Remove Assignments to Parameters
§ Replace Method with Method Object
§ Substitute Algorithm
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
23.08.2026, 11:10 Composing Methods
https://sourcemaking.com/refactoring/composing-methods 2/3

RETURN READ NEXT
Extract Method    Refactoring techniques
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
 Learn more
23.08.2026, 11:10 Composing Methods
https://sourcemaking.com/refactoring/composing-methods 3/3
