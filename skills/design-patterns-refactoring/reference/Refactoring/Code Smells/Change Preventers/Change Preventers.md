# Change Preventers

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells
Change Preventers
These smells mean that if you need to change something in one place in your code, you have to
make many changes in other places too. Program development becomes much more
complicated and expensive as a result.
You �nd yourself having to change many unrelated methods when you make changes to a
class. For example, when adding a new product type you have to change the methods for
�nding, displaying, and ordering products.
Making any modi�cations requires that you make many small changes to many different
classes.
Whenever you create a subclass for a class, you �nd yourself needing to create a subclass for
another class.
§Divergent Change
§Shotgun Surgery
§Parallel Inheritance Hierarchies
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more

 

READ NEXTRETURN
Divergent Change   Alternative Classes with Different
Interfaces
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
