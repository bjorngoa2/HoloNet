# Parameterize Method

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Parameterize Method
Problem
Multiple methods perform similar actions that are different only in their internal values, numbers
or operations.
Solution
Combine these methods by using a parameter that will pass the necessary special value.
Before
After

 
23.08.2026, 11:12 Parameterize Method
https://sourcemaking.com/refactoring/parameterize-method 1/3

Why Refactor
If you have similar methods, you probably have duplicate code, with all the consequences that this
entails.
What’s more, if you need to add yet another version of this functionality, you will have to create yet
another method. Instead, you could simply run the existing method with a different parameter.
Drawbacks
Sometimes this refactoring technique can be taken too far, resulting in a long and complicated
common method instead of multiple simpler ones.
Also be careful when moving activation/deactivation of functionality to a parameter. This can
eventually lead to creation of a large conditional operator that will need to be treated via
Replace Parameter with Explicit Methods.
How to Refactor
1. Create a new method with a parameter and move it to the code that is the same for all classes,
by applying Extract Method. Note that sometimes only a certain part of methods is actually the
same. In this case, refactoring consists of extracting only the same part to a new method.
2. In the code of the new method, replace the special/differing value with a parameter.
3. For each old method, find the places where it is called, replacing these calls with calls to the
new method that include a parameter. Then delete the old method.
23.08.2026, 11:12 Parameterize Method
https://sourcemaking.com/refactoring/parameterize-method 2/3

RETURN
READ NEXT
Replace Parameter with Explicit
Methods    Separate Query from Modifier
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
23.08.2026, 11:12 Parameterize Method
https://sourcemaking.com/refactoring/parameterize-method 3/3
