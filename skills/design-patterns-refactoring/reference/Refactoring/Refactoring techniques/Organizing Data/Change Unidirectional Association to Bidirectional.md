# Change Unidirectional Association to Bidirectional

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Organizing Data
Change UnidirectionalAssociation to Bidirectional
Problem
You have two classes that each need to use the features of the other, but the association between
them is only unidirectional.
Solution
Add the missing association to the class that needs it.
Before
After

 
23.08.2026, 11:11 Change Unidirectional Association to Bidirectional
https://sourcemaking.com/refactoring/change-unidirectional-association-to-bidirectional 1/3

Why Refactor
Originally the classes had a unidirectional association. But with time, client code needed access to
both sides of the association.
Benefits
If a class needs a reverse association, you can simply calculate it. But if these calculations are
complex, it is better to keep the reverse association.
Drawbacks
Bidirectional associations are much harder to implement and maintain than unidirectional ones.
Bidirectional associations make classes interdependent. With a unidirectional association, one of
them can be used independently of the other.
How to Refactor
1. Add a field for holding the reverse association.
2. Decide which class will be “dominant”. This class will contain the methods that create or update
the association as elements are added or changed, establishing the association in its class and
calling the utility methods for establishing the association in the associated object.
3. Create a utility method for establishing the association in the “non-dominant” class. The method
should use what it is given in parameters to complete the field. Give the method an obvious
name so that it is not used later for any other purposes.
4. If old methods for controlling the unidirectional association were in the “dominant” class,
complement them with calls to utility methods from the associated object.
5. If the old methods for controlling the association were in the “non-dominant” class, create the
methods in the “dominant” class, call them, and delegate execution to them.
Reading is boring
23.08.2026, 11:11 Change Unidirectional Association to Bidirectional
https://sourcemaking.com/refactoring/change-unidirectional-association-to-bidirectional 2/3

RETURN
READ NEXT
Change Bidirectional Association to
Unidirectional    Duplicate Observed Data
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:11 Change Unidirectional Association to Bidirectional
https://sourcemaking.com/refactoring/change-unidirectional-association-to-bidirectional 3/3
