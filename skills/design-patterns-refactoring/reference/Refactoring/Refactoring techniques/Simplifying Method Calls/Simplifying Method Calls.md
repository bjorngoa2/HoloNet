# Simplifying Method Calls

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques
Simplifying Method Calls
These techniques make method calls simpler and easier to understand. This, in turn, simplifies the
interfaces for interaction between classes.
Problem: The name of a method does not explain what the method does.
Solution: Rename the method.
Problem: A method does not have enough data to perform certain actions.
Solution: Create a new parameter to pass the necessary data.
Problem: A parameter is not used in the body of a method.
Solution: Remove the unused parameter.
Problem: Do you have a method that returns a value but also changes something inside an
object?
Solution: Split the method into two separate methods. As you would expect, one of them should
return the value and the other one modifies the object.
Problem: Multiple methods perform similar actions that are different only in their internal
values, numbers or operations.
§ Rename Method
§ Add Parameter
§ Remove Parameter
§ Separate Query from Modifier
§ Parameterize Method

 
23.08.2026, 11:11 Simplifying Method Calls
https://sourcemaking.com/refactoring/simplifying-method-calls 1/3

Solution: Combine these methods by using a parameter that will pass the necessary special
value.
Problem: A method is split into parts, each of which is run depending on the value of a
parameter.
Solution: Extract the individual parts of the method into their own methods and call them
instead of the original method.
Problem: You get several values from an object and then pass them as parameters to a method.
Solution: Instead, try passing the whole object.
Problem: Before a method call, a second method is run and its result is sent back to the first
method as an argument. But the parameter value could have been obtained inside the method
being called.
Solution: Instead of passing the value through a parameter, place the value-getting code inside
the method.
Problem: Your methods contain a repeating group of parameters.
Solution: Replace these parameters with an object.
Problem: The value of a field should be set only when it is created, and not change at any time
after that.
Solution: So remove methods that set the field’s value.
Problem: A method is not used by other classes or is used only inside its own class hierarchy.
Solution: Make the method private or protected.
§ Replace Parameter with Explicit Methods
§ Preserve Whole Object
§ Replace Parameter with Method Call
§ Introduce Parameter Object
§ Remove Setting Method
§ Hide Method
§ Replace Constructor with Factory Method
23.08.2026, 11:11 Simplifying Method Calls
https://sourcemaking.com/refactoring/simplifying-method-calls 2/3

Problem: You have a complex constructor that does something more than just setting parameter
values in object fields.
Solution: Create a factory method and use it to replace constructor calls.
Problem: A method returns a special value that indicates an error?
Solution: Throw an exception instead.
Problem: You throw an exception in a place where a simple test would do the job?
Solution: Replace the exception with a condition test.
RETURN READ NEXT
§ Replace Error Code with Exception
§ Replace Exception with Test
Rename Method    Introduce Assertion
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
23.08.2026, 11:11 Simplifying Method Calls
https://sourcemaking.com/refactoring/simplifying-method-calls 3/3
