# Hide Method

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Hide Method
Problem
A method is not used by other classes or is used only inside its own class hierarchy.
Solution
Make the method private or protected.
Before
After

 
23.08.2026, 11:12 Hide Method
https://sourcemaking.com/refactoring/hide-method 1/3

Why Refactor
Quite often, the need to hide methods for getting and setting values is due to development of a
richer interface that provides additional behavior, especially if you started with a class that added
little beyond mere data encapsulation.
As new behavior is built into the class, you may find that public getter and setter methods are no
longer necessary and can be hidden. If you make getter or setter methods private and apply direct
access to variables, you can delete the method.
Benefits
Hiding methods makes it easier for your code to evolve. When you change a private method, you
only need to worry about how to not break the current class since you know that the method
cannot be used anywhere else.
By making methods private, you underscore the importance of the public interface of the class
and of the methods that remain public.
How to Refactor
1. Regularly try to find methods that can be made private. Static code analysis and good unit test
coverage can offer a big leg up.
2. Make each method as private as possible.
RETURN
READ NEXT
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:12 Hide Method
https://sourcemaking.com/refactoring/hide-method 2/3

Replace Constructor with Factory
Method    Remove Setting Method
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:12 Hide Method
https://sourcemaking.com/refactoring/hide-method 3/3
