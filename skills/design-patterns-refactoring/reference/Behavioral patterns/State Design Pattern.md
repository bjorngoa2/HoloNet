# State Design Pattern

Source: sourcemaking.com (downloaded copy)

---

 / Design Patterns / Behavioral patterns
State Design Pattern
Intent
• Allow an object to alter its behavior when its internal state changes. The object will appear
to change its class.
• An object-oriented state machine
• wrapper + polymorphic wrappee + collaboration
Problem
A monolithic object's behavior is a function of its state, and it must change its behavior at run-
time depending on that state. Or, an application is characterized by large and numerous case
statements that vector �ow of control based on the state of the application.
Discussion
The State pattern is a solution to the problem of how to make behavior depend on state.
• De�ne a "context" class to present a single interface to the outside world.
• De�ne a State abstract base class.
• Represent the different "states" of the state machine as derived classes of the State base
class.
• De�ne state-speci�c behavior in the appropriate State derived classes.
• Maintain a pointer to the current "state" in the "context" class.
• T o change the state of the state machine, change the current "state" pointer.

 

The State pattern does not specify where the state transitions will be de�ned. The choices are
two: the "context" object, or each individual State derived class. The advantage of the latter
option is ease of adding new State derived classes. The disadvantage is each State derived class
has knowledge of (coupling to) its siblings, which introduces dependencies between subclasses.
A table-driven approach to designing �nite state machines does a good job of specifying state
transitions, but it is dif�cult to add actions to accompany the state transitions. The pattern-
based approach uses code (instead of data structures) to specify state transitions, but it does a
good job of accommodating state transition actions.
Structure
The state machine's interface is encapsulated in the "wrapper" class. The wrappee hierarchy's
interface mirrors the wrapper's interface with the exception of one additional parameter. The
extra parameter allows wrappee derived classes to call back to the wrapper class as necessary.
Complexity that would otherwise drag down the wrapper class is neatly compartmented and
encapsulated in a polymorphic hierarchy to which the wrapper object delegates.
Example

The State pattern allows an object to change its behavior when its internal state changes. This
pattern can be observed in a vending machine. Vending machines have states based on the
inventory, amount of currency deposited, the ability to make change, the item selected, etc.
When currency is deposited and a selection is made, a vending machine will either deliver a
product and no change, deliver a product and change, deliver no product due to insuf�cient
currency on deposit, or deliver no product due to inventory depletion.
Check list
1. Identify an existing class, or create a new class, that will serve as the "state machine" from
the client's perspective. That class is the "wrapper" class.
2. Create a State base class that replicates the methods of the state machine interface. Each
method takes one additional parameter: an instance of the wrapper class. The State base
class speci�es any useful "default" behavior.
3. Create a State derived class for each domain state. These derived classes only override the
methods they need to override.
4. The wrapper class maintains a "current" State object.
5. All client requests to the wrapper class are simply delegated to the current State object, and
the wrapper object's this  pointer is passed.
6. The State methods change the "current" state in the wrapper object as appropriate.

Rules of thumb
• State objects are often Singletons.
• Flyweight explains when and how State objects can be shared.
• Interpreter can use State to de�ne parsing contexts.
• Strategy has 2 different implementations, the �rst is similar to State. The difference is in
binding times (Strategy is a bind-once pattern, whereas State is more dynamic).
• The structure of State and Bridge are identical (except that Bridge admits hierarchies of
envelope classes, whereas State allows only one). The two patterns use the same structure
to solve different problems: State allows an object's behavior to change along with its state,
while Bridge's intent is to decouple an abstraction from its implementation so that the two
can vary independently.
• The implementation of the State pattern builds on the Strategy pattern. The difference
between State and Strategy is in the intent. With Strategy, the choice of algorithm is fairly
stable. With State, a change in the state of the "context" object causes it to select from its
"palette" of Strategy objects.
Code examples
Java
State in
Java: Before
and after
State in Java: Case
statement
considered harmful
State
in Java
State
in Java
State
in Java
State in Java:
Distributed
transition logic
State
in Java
Support our free website and own the eBook!
• 22 design patterns and 8 principles explained in depth
• 406 well-structured, easy to read, jargon-free pages
• 228 clear and helpful illustrations and diagrams
• An archive with code examples in 4 languages
• All devices supported: EPUB/MOBI/PDF formats
 Learn more...

C++State in C++
PHPState in PHP
DelphiState in
Delphi State in Delphi
PythonState in
Python
READ NEXTRETURN
Strategy Design Pattern  ObserverDesign Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
