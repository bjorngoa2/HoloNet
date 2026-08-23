# Facade Design Pattern

Source: sourcemaking.com (downloaded copy)

---

 / Design Patterns / Structural patterns
Facade Design Pattern
Intent
• Provide a uni�ed interface to a set of interfaces in a subsystem. Facade de�nes a higher-
level interface that makes the subsystem easier to use.
• Wrap a complicated subsystem with a simpler interface.
Problem
A segment of the client community needs a simpli�ed interface to the overall functionality of a
complex subsystem.
Discussion
Facade discusses encapsulating a complex subsystem within a single interface object. This
reduces the learning curve necessary to successfully leverage the subsystem. It also promotes
decoupling the subsystem from its potentially many clients. On the other hand, if the Facade is
the only access point for the subsystem, it will limit the features and �exibility that "power
users" may need.
The Facade object should be a fairly simple advocate or facilitator. It should not become an all-
knowing oracle or "god" object.
Structure
Facade takes a "riddle wrapped in an enigma shrouded in mystery", and interjects a wrapper

 

that tames the amorphous and inscrutable mass of software.
SubsystemOne  and
SubsystemThree  do not interact with the internal components of
SubsystemTwo .
They use the
SubsystemTwoWrapper  "facade" (i.e. the higher level abstraction).

Example
The Facade de�nes a uni�ed, higher level interface to a subsystem that makes it easier to use.
Consumers encounter a Facade when ordering from a catalog. The consumer calls one number
and speaks with a customer service representative. The customer service representative acts as
a Facade, providing an interface to the order ful�llment department, the billing department,
and the shipping department.

Check list
1. Identify a simpler, uni�ed interface for the subsystem or component.
2. Design a 'wrapper' class that encapsulates the subsystem.
3. The facade/wrapper captures the complexity and collaborations of the component, and
delegates to the appropriate methods.
4. The client uses (is coupled to) the Facade only.
5. Consider whether additional Facades would add value.
Rules of thumb
• Facade de�nes a new interface, whereas Adapter uses an old interface. Remember that
Adapter makes two existing interfaces work together as opposed to de�ning an entirely new
one.
• Whereas Flyweight shows how to make lots of little objects, Facade shows how to make a
single object represent an entire subsystem.
• Mediator is similar to Facade in that it abstracts functionality of existing classes. Mediator
abstracts/centralizes arbitrary communications between colleague objects. It routinely "adds
value", and it is known/referenced by the colleague objects. In contrast, Facade de�nes a
simpler interface to a subsystem, it doesn't add new functionality, and it is not known by the
subsystem classes.
• Abstract Factory can be used as an alternative to Facade to hide platform-speci�c classes.

• Facade objects are often Singletons because only one Facade object is required.
• Adapter and Facade are both wrappers; but they are different kinds of wrappers. The intent
of Facade is to produce a simpler interface, and the intent of Adapter is to design to an
existing interface. While Facade routinely wraps multiple objects and Adapter wraps a single
object; Facade could front-end a single complex object and Adapter could wrap several
legacy objects.
Question: So the way to tell the difference between the Adapter pattern and the Facade pattern
is that the Adapter wraps one class and the Facade may represent many classes?
Answer: No! Remember, the Adapter pattern changes the interface of one or more classes into
one interface that a client is expecting. While most textbook examples show the adapter
adapting one class, you may need to adapt many classes to provide the interface a client is
coded to. Likewise, a Facade may provide a simpli�ed interface to a single class with a very
complex interface. The difference between the two is not in terms of how many classes they
"wrap", it is in their intent.
Code examples
JavaFacade in Java
C++Facade in C++
PHPFacade in PHP
DelphiFacade in Delphi
PythonFacade in Python
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

READ NEXTRETURN
Flyweight Design Pattern  Decorator
