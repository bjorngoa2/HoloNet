# Organizing Data

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques
Organizing Data
These refactoring techniques help with data handling, replacing primitives with rich class
functionality.
Another important result is untangling of class associations, which makes classes more portable
and reusable.
Problem: You use direct access to private fields inside a class.
Solution: Create a getter and setter for the field, and use only them for accessing the field.
Problem: A class (or group of classes) contains a data field. The field has its own behavior and
associated data.
Solution: Create a new class, place the old field and its behavior in the class, and store the object
of the class in the original class.
Problem: So you have many identical instances of a single class that you need to replace with a
single object.
Solution: Convert the identical objects to a single reference object.
Problem: You have a reference object that is too small and infrequently changed to justify
managing its life cycle.
§ Self Encapsulate Field
§ Replace Data Value with Object
§ Change Value to Reference
§ Change Reference to Value

 
23.08.2026, 11:10 Organizing Data
https://sourcemaking.com/refactoring/organizing-data 1/4

Solution: Turn it into a value object.
Problem: You have an array that contains various types of data.
Solution: Replace the array with an object that will have separate fields for each element.
Problem: Is domain data stored in classes responsible for the GUI?
Solution: Then it is a good idea to separate the data into separate classes, ensuring connection
and synchronization between the domain class and the GUI.
Problem: You have two classes that each need to use the features of the other, but the
association between them is only unidirectional.
Solution: Add the missing association to the class that needs it.
Problem: You have a bidirectional association between classes, but one of the classes does not
use the other’s features.
Solution: Remove the unused association.
Problem: Your code uses a number that has a certain meaning to it.
Solution: Replace this number with a constant that has a human-readable name explaining the
meaning of the number.
Problem: You have a public field.
Solution: Make the field private and create access methods for it.
Problem: A class contains a collection field and a simple getter and setter for working with the
collection.
§ Replace Array with Object
§ Duplicate Observed Data
§ Change Unidirectional Association to Bidirectional
§ Change Bidirectional Association to Unidirectional
§ Replace Magic Number with Symbolic Constant
§ Encapsulate Field
§ Encapsulate Collection
23.08.2026, 11:10 Organizing Data
https://sourcemaking.com/refactoring/organizing-data 2/4

Solution: Make the getter-returned value read-only and create methods for adding/deleting
elements of the collection.
Problem: A class has a field that contains type code. The values of this type are not used in
operator conditions and do not affect the behavior of the program.
Solution: Create a new class and use its objects instead of the type code values.
Problem: You have a coded type that directly affects program behavior (values of this field
trigger various code in conditionals).
Solution: Create subclasses for each value of the coded type. Then extract the relevant behaviors
from the original class to these subclasses. Replace the control flow code with polymorphism.
Problem: You have a coded type that affects behavior but you cannot use subclasses to get rid of
it.
Solution: Replace type code with a state object. If it is necessary to replace a field value with
type code, another state object is “plugged in”.
Problem: You have subclasses differing only in their (constant-returning) methods.
Solution: Replace the methods with fields in the parent class and delete the subclasses.
RETURN READ NEXT
§ Replace Type Code with Class
§ Replace Type Code with Subclasses
§ Replace Type Code with State/Strategy
§ Replace Subclass with Fields
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:10 Organizing Data
https://sourcemaking.com/refactoring/organizing-data 3/4

Self Encapsulate Field    Introduce Local Extension
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:10 Organizing Data
https://sourcemaking.com/refactoring/organizing-data 4/4
