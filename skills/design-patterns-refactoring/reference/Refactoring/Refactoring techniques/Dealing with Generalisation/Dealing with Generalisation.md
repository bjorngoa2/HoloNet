# Dealing with Generalisation

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques
Dealing with Generalisation
Abstraction has its own group of refactoring techniques, primarily associated with moving
functionality along the class inheritance hierarchy, creating new classes and interfaces, and
replacing inheritance with delegation and vice versa.
Problem: Two classes have the same field.
Solution: Remove the field from subclasses and move it to the superclass.
Problem: Your subclasses have methods that perform similar work.
Solution: Make the methods identical and then move them to the relevant superclass.
Problem: Your subclasses have constructors with code that is mostly identical.
Solution: Create a superclass constructor and move the code that is the same in the subclasses
to it. Call the superclass constructor in the subclass constructors.
Problem: Is behavior implemented in a superclass used by only one (or a few) subclasses?
Solution: Move this behavior to the subclasses.
Problem: Is a field used only in a few subclasses?
§ Pull Up Field
§ Pull Up Method
§ Pull Up Constructor Body
§ Push Down Method
§ Push Down Field

 
23.08.2026, 11:12 Dealing with Generalisation
https://sourcemaking.com/refactoring/dealing-with-generalisation 1/3

Solution: Move the field to these subclasses.
Problem: A class has features that are used only in certain cases.
Solution: Create a subclass and use it in these cases.
Problem: You have two classes with common fields and methods.
Solution: Create a shared superclass for them and move all the identical fields and methods to
it.
Problem: Multiple clients are using the same part of a class interface. Another case: part of the
interface in two classes is the same.
Solution: Move this identical portion to its own interface.
Problem: You have a class hierarchy in which a subclass is practically the same as its superclass.
Solution: Merge the subclass and superclass.
Problem: Your subclasses implement algorithms that contain similar steps in the same order.
Solution: Move the algorithm structure and identical steps to a superclass, and leave
implementation of the different steps in the subclasses.
Problem: You have a subclass that uses only a portion of the methods of its superclass (or it’s not
possible to inherit superclass data).
Solution: Create a field and put a superclass object in it, delegate methods to the superclass
object, and get rid of inheritance.
Problem: A class contains many simple methods that delegate to all methods of another class.
§ Extract Subclass
§ Extract Superclass
§ Extract Interface
§ Collapse Hierarchy
§ Form Template Method
§ Replace Inheritance with Delegation
§ Replace Delegation with Inheritance
23.08.2026, 11:12 Dealing with Generalisation
https://sourcemaking.com/refactoring/dealing-with-generalisation 2/3

Solution: Make the class a delegate inheritor, which makes the delegating methods unnecessary.
RETURN READ NEXT
Pull Up Field    Replace Exception with Test
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
23.08.2026, 11:12 Dealing with Generalisation
https://sourcemaking.com/refactoring/dealing-with-generalisation 3/3
