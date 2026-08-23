# Temporary Field

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Code Smells / Object-Orientation Abusers
T emporary Field
Signs and Symptoms
T emporary �elds get their values (and thus are needed by objects) only under certain
circumstances. Outside of these circumstances, they are empty.
Reasons for the Problem
Oftentimes, temporary �elds are created for use in an algorithm that requires a large amount of
inputs. So instead of creating a large number of parameters in the method, the programmer
decides to create �elds for this data in the class. These �elds are used only in the algorithm and
go unused the rest of the time.
This kind of code is tough to understand. You expect to see data in object �elds but for some
reason they are almost always empty.

 

Treatment
T emporary �elds and all code operating on them can be put in a separate class via Extract
Class. In other words, you are creating a method object, achieving the same result as if you
would perform Replace Method with Method Object.
Introduce Null Object and integrate it in place of the conditional code which was used to check
the temporary �eld values for existence.
Payoff
Better code clarity and organization.
Reading is boring
Aren't you bored of reading so much? Try out our new interactive

READ NEXTRETURN
Refused Bequest  Switch Statements
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
learning course on refactoring. It has more content and much more
fun.
 Learn more
