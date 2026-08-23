# Remove Control Flag

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Conditional Expressions
Remove Control Flag
Problem
You have a boolean variable that acts as a control flag for multiple boolean expressions.
Solution
Instead of the variable, use break, continue and return.
Why Refactor
Control flags date back to the days of yore, when “proper” programmers always had one entry point
for their functions (the function declaration line) and one exit point (at the very end of the
function).
In modern programming languages this style tic is obsolete, since we have special operators for
modifying the control flow in loops and other complex constructions:
break: stops loop
continue: stops execution of the current loop branch and goes to check the loop conditions in
the next iteration
return: stops execution of the entire function and returns its result if given in the operator
Benefits
Control flag code is often much more ponderous than code written with control flow operators.

 
23.08.2026, 11:11 Remove Control Flag
https://sourcemaking.com/refactoring/remove-control-flag 1/2

How to Refactor
1. Find the value assignment to the control flag that causes the exit from the loop or current
iteration.
2. Replace it with break, if this is an exit from a loop; continue, if this is an exit from an iteration,
or return, if you need to return this value from the function.
3. Remove the remaining code and checks associated with the control flag.
RETURN READ NEXT
Replace Nested Conditional with Guard
Clauses    Consolidate Duplicate Conditional
Fragments
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
23.08.2026, 11:11 Remove Control Flag
https://sourcemaking.com/refactoring/remove-control-flag 2/2
