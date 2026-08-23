# Pull Up Method

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Dealing with Generalisation
Pull Up Method
Problem
Your subclasses have methods that perform similar work.
Solution
Make the methods identical and then move them to the relevant superclass.
Before
After

 
23.08.2026, 11:12 Pull Up Method
https://sourcemaking.com/refactoring/pull-up-method 1/3

Why Refactor
Subclasses grew and developed independently of one another, causing identical (or nearly
identical) fields and methods.
Benefits
Gets rid of duplicate code. If you need to make changes to a method, it’s better to do so in a
single place than have to search for all duplicates of the method in subclasses.
This refactoring technique can also be used if, for some reason, a subclass redefines a superclass
method but performs what is essentially the same work.
How to Refactor
1. Investigate similar methods in superclasses. If they are not identical, format them to match each
other.
23.08.2026, 11:12 Pull Up Method
https://sourcemaking.com/refactoring/pull-up-method 2/3

2. If methods use a different set of parameters, put the parameters in the form that you want to
see in the superclass.
3. Copy the method to the superclass. Here you may find that the method code uses fields and
methods that exist only in subclasses and therefore are not available in the superclass. To solve
this, you can:
For fields: use either Pull Up Field or Self-Encapsulate Field to create getters and setters in
subclasses; then declare these getters abstractly in the superclass.
For methods: use either Pull Up Method or declare abstract methods for them in the superclass
(note that your class will become abstract if it was not previously).
4. Remove the methods from the subclasses.
5. Check the locations in which the method is called. In some places you may be able to replace
use of a subclass with the superclass.
RETURN READ NEXT
Pull Up Constructor Body    Pull Up Field
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
23.08.2026, 11:12 Pull Up Method
https://sourcemaking.com/refactoring/pull-up-method 3/3
