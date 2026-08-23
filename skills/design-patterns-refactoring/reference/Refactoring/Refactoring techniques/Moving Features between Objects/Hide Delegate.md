# Hide Delegate

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Moving Features between Objects
Hide Delegate
Problem
The client gets object B from a field or method of object А . Then the client calls a method of object
B.
Solution
Create a new method in class A that delegates the call to object B. Now the client does not know
about, or depend on, class B.
Before
After

 
23.08.2026, 11:10 Hide Delegate
https://sourcemaking.com/refactoring/hide-delegate 1/3

Why Refactor
To start with, let’s look at terminology:
delegate — the end object that contains the functionality needed by the client
server — the object to which the client has direct access
A call chain appears when a client requests an object from another object, then the second object
requests another one, and so on. These sequences of calls involve the client in navigation along
the class structure. Any changes in these interrelationships will require changes on the client side.
Benefits
Hides delegation from the client. The less that the client code needs to know about the details of
relationships between objects, the easier it is to make changes to your program.
Drawbacks
23.08.2026, 11:10 Hide Delegate
https://sourcemaking.com/refactoring/hide-delegate 2/3

If you need to create an excessive number of delegating methods, server-class risks becoming an
unneeded go-between, leading to an excess of Middle Man.
How to Refactor
1. For each method of the delegate-class called by the client, create a method in the server-class
that delegates the call to the delegate-class.
2. Change the client code so that it calls the methods of the server-class.
3. If your changes free the client from needing the delegate-class, you can remove the access
method to the delegate-class from the server-class (the method that was originally used to get
the delegate-class).
RETURN READ NEXT
Remove Middle Man    Inline Class
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
23.08.2026, 11:10 Hide Delegate
https://sourcemaking.com/refactoring/hide-delegate 3/3
