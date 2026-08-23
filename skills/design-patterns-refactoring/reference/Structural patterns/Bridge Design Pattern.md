# Bridge Design Pattern

Source: sourcemaking.com (downloaded copy)

---

 / Design Patterns / Structural patterns
Bridge Design Pattern
Intent
• Decouple an abstraction from its implementation so that the two can vary independently.
• Publish interface in an inheritance hierarchy, and bury implementation in its own inheritance
hierarchy.
• Beyond encapsulation, to insulation
Problem
"Hardening of the software arteries" has occurred by using subclassing of an abstract base class
to provide alternative implementations. This locks in compile-time binding between interface
and implementation. The abstraction and implementation cannot be independently extended or
composed.
Motivation
Consider the domain of "thread scheduling".

 

There are two types of thread schedulers, and two types of operating systems or "platforms".
Given this approach to specialization, we have to de�ne a class for each permutation of these
two dimensions. If we add a new platform (say ... Java's Virtual Machine), what would our
hierarchy look like?
What if we had three kinds of thread schedulers, and four kinds of platforms? What if we had
�ve kinds of thread schedulers, and ten kinds of platforms? The number of classes we would
have to de�ne is the product of the number of scheduling schemes and the number of
platforms.
The Bridge design pattern proposes refactoring this exponentially explosive inheritance
hierarchy into two orthogonal hierarchies – one for platform-independent abstractions, and the
other for platform-dependent implementations.
Discussion
Decompose the component's interface and implementation into orthogonal class hierarchies.
The interface class contains a pointer to the abstract implementation class. This pointer is

initialized with an instance of a concrete implementation class, but all subsequent interaction
from the interface class to the implementation class is limited to the abstraction maintained in
the implementation base class. The client interacts with the interface class, and it in turn
"delegates" all requests to the implementation class.
The interface object is the "handle" known and used by the client; while the implementation
object, or "body", is safely encapsulated to ensure that it may continue to evolve, or be entirely
replaced (or shared at run-time.
Use the Bridge pattern when:
• you want run-time binding of the implementation,
• you have a proliferation of classes resulting from a coupled interface and numerous
implementations,
• you want to share an implementation among multiple objects,
• you need to map orthogonal class hierarchies.
Consequences include:
• decoupling the object's interface,
• improved extensibility (you can extend (i.e. subclass) the abstraction and implementation
hierarchies independently),
• hiding details from clients.
Bridge is a synonym for the "handle/body" idiom. This is a design mechanism that encapsulates
an implementation class inside of an interface class. The former is the body, and the latter is
the handle. The handle is viewed by the user as the actual class, but the work is done in the
body. "The handle/body class idiom may be used to decompose a complex abstraction into
smaller, more manageable classes. The idiom may re�ect the sharing of a single resource by
multiple classes that control access to it (e.g. reference counting)."
Structure
The Client doesn't want to deal with platform-dependent details. The Bridge pattern
encapsulates this complexity behind an abstraction "wrapper".
Bridge emphasizes identifying and decoupling "interface" abstraction from "implementation"

abstraction.
Example
The Bridge pattern decouples an abstraction from its implementation, so that the two can vary
independently. A household switch controlling lights, ceiling fans, etc. is an example of the
Bridge. The purpose of the switch is to turn a device on or off. The actual switch can be
implemented as a pull chain, simple two position switch, or a variety of dimmer switches.

Check list
1. Decide if two orthogonal dimensions exist in the domain. These independent concepts could
be: abstraction/platform, or domain/infrastructure, or front-end/back-end, or interface/
implementation.
2. Design the separation of concerns: what does the client want, and what do the platforms
provide.
3. Design a platform-oriented interface that is minimal, necessary, and suf�cient. Its goal is to
decouple the abstraction from the platform.
4. De�ne a derived class of that interface for each platform.
5. Create the abstraction base class that "has a" platform object and delegates the platform-
oriented functionality to it.
6. De�ne specializations of the abstraction class if desired.
Rules of thumb
• Adapter makes things work after they're designed; Bridge makes them work before they are.
• Bridge is designed up-front to let the abstraction and the implementation vary
independently. Adapter is retro�tted to make unrelated classes work together.
• State, Strategy, Bridge (and to some degree Adapter) have similar solution structures. They

all share elements of the "handle/body" idiom. They differ in intent - that is, they solve
different problems.
• The structure of State and Bridge are identical (except that Bridge admits hierarchies of
envelope classes, whereas State allows only one). The two patterns use the same structure
to solve different problems: State allows an object's behavior to change along with its state,
while Bridge's intent is to decouple an abstraction from its implementation so that the two
can vary independently.
• If interface classes delegate the creation of their implementation classes (instead of
creating/coupling themselves directly), then the design usually uses the Abstract Factory
pattern to create the implementation objects.
Code examples
JavaBridge in JavaBridge in JavaBridge in Java
C++Bridge in C++
PHPBridge in PHP
PythonBridge in Python
READ NEXTRETURN
Composite Design Pattern  Adapter
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
Support our free website and own the eBook!
• 22 design patterns and 8 principles explained in depth
• 406 well-structured, easy to read, jargon-free pages
• 228 clear and helpful illustrations and diagrams
• An archive with code examples in 4 languages
• All devices supported: EPUB/MOBI/PDF formats
 Learn more...
