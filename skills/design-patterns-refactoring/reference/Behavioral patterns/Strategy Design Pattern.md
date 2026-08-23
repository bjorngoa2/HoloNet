# Strategy Design Pattern

Source: sourcemaking.com (downloaded copy)

---

 / Design Patterns / Behavioral patterns
Strategy Design Pattern
Intent
• De�ne a family of algorithms, encapsulate each one, and make them interchangeable.
Strategy lets the algorithm vary independently from the clients that use it.
• Capture the abstraction in an interface, bury implementation details in derived classes.
Problem
One of the dominant strategies of object-oriented design is the "open-closed principle".
Figure demonstrates how this is routinely achieved - encapsulate interface details in a base
class, and bury implementation details in derived classes. Clients can then couple themselves
to an interface, and not have to experience the upheaval associated with change: no impact
when the number of derived classes changes, and no impact when the implementation of a
derived class changes.

 

A generic value of the software community for years has been, "maximize cohesion and
minimize coupling". The object-oriented design approach shown in �gure is all about
minimizing coupling. Since the client is coupled only to an abstraction (i.e. a useful �ction), and
not a particular realization of that abstraction, the client could be said to be practicing "abstract
coupling" . an object-oriented variant of the more generic exhortation "minimize coupling".
A more popular characterization of this "abstract coupling" principle is "Program to an interface,
not an implementation".
Clients should prefer the "additional level of indirection" that an interface (or an abstract base
class) affords. The interface captures the abstraction (i.e. the "useful �ction") the client wants to
exercise, and the implementations of that interface are effectively hidden.
Structure
The Interface entity could represent either an abstract base class, or the method signature
expectations by the client. In the former case, the inheritance hierarchy represents dynamic
polymorphism. In the latter case, the Interface entity represents template code in the client and
the inheritance hierarchy represents static polymorphism.
Example
A Strategy de�nes a set of algorithms that can be used interchangeably. Modes of

transportation to an airport is an example of a Strategy. Several options exist such as driving
one's own car, taking a taxi, an airport shuttle, a city bus, or a limousine service. For some
airports, subways and helicopters are also available as a mode of transportation to the airport.
Any of these modes of transportation will get a traveler to the airport, and they can be used
interchangeably. The traveler must choose the Strategy based on trade-offs between cost,
convenience, and time.
Check list
1. Identify an algorithm (i.e. a behavior) that the client would prefer to access through a "�ex
point".
2. Specify the signature for that algorithm in an interface.
3. Bury the alternative implementation details in derived classes.
4. Clients of the algorithm couple themselves to the interface.
Rules of thumb
• Strategy is like T emplate Method except in its granularity.
• State is like Strategy except in its intent.
• Strategy lets you change the guts of an object. Decorator lets you change the skin.

• State, Strategy, Bridge (and to some degree Adapter) have similar solution structures. They
all share elements of the 'handle/body' idiom. They differ in intent - that is, they solve
different problems.
• Strategy has 2 different implementations, the �rst is similar to State. The difference is in
binding times (Strategy is a bind-once pattern, whereas State is more dynamic).
• Strategy objects often make good Flyweights.
Code examples
JavaStrategy in Java
C++Strategy in C++
PHPStrategy in PHP
DelphiStrategy in Delphi
PythonStrategy in Python
READ NEXTRETURN
T emplate Method Design Pattern  StateDesign Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com T erms / Privacy policy
Support our free website and own the eBook!
• 22 design patterns and 8 principles explained in depth
• 406 well-structured, easy to read, jargon-free pages
• 228 clear and helpful illustrations and diagrams
• An archive with code examples in 4 languages
• All devices supported: EPUB/MOBI/PDF formats
 Learn more...

All rights reserved.
