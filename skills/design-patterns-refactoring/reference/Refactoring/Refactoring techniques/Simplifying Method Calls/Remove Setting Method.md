# Remove Setting Method

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Remove Setting Method
Problem
The value of a field should be set only when it is created, and not change at any time after that.
Solution
So remove methods that set the field’s value.
Before
After

 
23.08.2026, 11:12 Remove Setting Method
https://sourcemaking.com/refactoring/remove-setting-method 1/2

Why Refactor
You want to prevent any changes to the value of a field.
How to Refactor
1. The value of a field should be changeable only in the constructor. If the constructor does not
contain a parameter for setting the value, add one.
2. Find all setter calls.
If a setter call is located right after a call for the constructor of the current class, move its
argument to the constructor call and remove the setter.
Replace setter calls in the constructor with direct access to the field.
Delete the setter.
RETURN READ NEXT
Hide Method    Introduce Parameter Object
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
23.08.2026, 11:12 Remove Setting Method
https://sourcemaking.com/refactoring/remove-setting-method 2/2
