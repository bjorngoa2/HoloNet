# Pull Up Field

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Dealing with Generalisation
Pull Up Field
Problem
Two classes have the same field.
Solution
Remove the field from subclasses and move it to the superclass.
Before
After

 
23.08.2026, 11:12 Pull Up Field
https://sourcemaking.com/refactoring/pull-up-field 1/3

Why Refactor
Subclasses grew and developed separately, causing identical (or nearly identical) fields and
methods to appear.
Benefits
Eliminates duplication of fields in subclasses.
Eases subsequent relocation of duplicate methods, if they exist, from subclasses to a superclass.
How to Refactor
1. Make sure that the fields are used for the same needs in subclasses.
2. If the fields have different names, give them the same name and replace all references to the
fields in existing code.
23.08.2026, 11:12 Pull Up Field
https://sourcemaking.com/refactoring/pull-up-field 2/3

3. Create a field with the same name in the superclass. Note that if the fields were private, the
superclass field should be protected.
4. Remove the fields from the subclasses.
5. You may want to consider using Self Encapsulate Field for the new field, in order to hide it
behind access methods.
RETURN READ NEXT
Pull Up Method    Dealing with Generalisation
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
23.08.2026, 11:12 Pull Up Field
https://sourcemaking.com/refactoring/pull-up-field 3/3
