# Introduce Parameter Object

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Introduce Parameter Object
Problem
Your methods contain a repeating group of parameters.
Solution
Replace these parameters with an object.
Before
After

 
23.08.2026, 11:12 Introduce Parameter Object
https://sourcemaking.com/refactoring/introduce-parameter-object 1/3

Why Refactor
Identical groups of parameters are often encountered in multiple methods. This causes code
duplication of both the parameters themselves and of related operations. By consolidating
parameters in a single class, you can also move the methods for handling this data there as well,
freeing the other methods from this code.
Benefits
More readable code. Instead of a hodgepodge of parameters, you see a single object with a
comprehensible name.
Identical groups of parameters scattered here and there create their own kind of code
duplication: while identical code is not being called, identical groups of parameters and
arguments are constantly encountered.
Drawbacks
If you move only data to a new class and do not plan to move any behaviors or related operations
there, this begins to smell of a Data Class.
How to Refactor
1. Create a new class that will represent your group of parameters. Make the class immutable.
23.08.2026, 11:12 Introduce Parameter Object
https://sourcemaking.com/refactoring/introduce-parameter-object 2/3

2. In the method that you want to refactor, use Add Parameter, which is where your parameter
object will be passed. In all method calls, pass the object created from old method parameters
to this parameter.
3. Now start deleting old parameters from the method one by one, replacing them in the code
with fields of the parameter object. Test the program after each parameter replacement.
4. When done, see whether there is any point in moving a part of the method (or sometimes even
the whole method) to a parameter object class. If so, use Move Method or Extract Method.
RETURN READ NEXT
Remove Setting Method    Replace Parameter with Method Call
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
23.08.2026, 11:12 Introduce Parameter Object
https://sourcemaking.com/refactoring/introduce-parameter-object 3/3
