# Iterator Design Pattern

Source: sourcemaking.com (downloaded copy)

---

 / Design Patterns / Behavioral patterns
Iterator Design Pattern
Intent
• Provide a way to access the elements of an aggregate object sequentially without exposing
its underlying representation.
• The C++ and Java standard library abstraction that makes it possible to decouple collection
classes and algorithms.
• Promote to "full object status" the traversal of a collection.
• Polymorphic traversal
Problem
Need to "abstract" the traversal of wildly different data structures so that algorithms can be
de�ned that are capable of interfacing with each transparently.
Discussion
"An aggregate object such as a list should give you a way to access its elements without
exposing its internal structure. Moreover, you might want to traverse the list in different ways,
depending on what you need to accomplish. But you probably don't want to bloat the List
interface with operations for different traversals, even if you could anticipate the ones you'll
require. You might also need to have more than one traversal pending on the same list." And,
providing a uniform interface for traversing many types of aggregate objects (i.e. polymorphic
iteration) might be valuable.
The Iterator pattern lets you do all this. The key idea is to take the responsibility for access and
traversal out of the aggregate object and put it into an Iterator object that de�nes a standard

 

traversal protocol.
The Iterator abstraction is fundamental to an emerging technology called "generic
programming". This strategy seeks to explicitly separate the notion of "algorithm" from that of
"data structure". The motivation is to: promote component-based development, boost
productivity, and reduce con�guration management.
As an example, if you wanted to support four data structures (array, binary tree, linked list, and
hash table) and three algorithms (sort, �nd, and merge), a traditional approach would require
four times three permutations to develop and maintain. Whereas, a generic programming
approach would only require four plus three con�guration items.
Structure
The Client uses the Collection class' public interface directly. But access to the Collection's
elements is encapsulated behind the additional level of abstraction called Iterator. Each
Collection derived class knows which Iterator derived class to create and return. After that, the
Client relies on the interface de�ned in the Iterator base class.
Example

The Iterator provides ways to access elements of an aggregate object sequentially without
exposing the underlying structure of the object. Files are aggregate objects. In of�ce settings
where access to �les is made through administrative or secretarial staff, the Iterator pattern is
demonstrated with the secretary acting as the Iterator. Several television comedy skits have
been developed around the premise of an executive trying to understand the secretary's �ling
system. T o the executive, the �ling system is confusing and illogical, but the secretary is able to
access �les quickly and ef�ciently.
On early television sets, a dial was used to change channels. When channel sur�ng, the viewer
was required to move the dial through each channel position, regardless of whether or not that
channel had reception. On modern television sets, a next and previous button are used. When
the viewer selects the "next" button, the next tuned channel will be displayed. Consider
watching television in a hotel room in a strange city. When sur�ng through channels, the
channel number is not important, but the programming is. If the programming on one channel
is not of interest, the viewer can request the next channel, without knowing its number.
Check list
1. Add a create_iterator()  method to the "collection" class, and grant the "iterator" class
privileged access.
2. Design an "iterator" class that can encapsulate traversal of the "collection" class.
3. Clients ask the collection object to create an iterator object.
4. Clients use the first() , is_done() , next() , and current_item()  protocol to access the
elements of the collection class.
Rules of thumb

• The abstract syntax tree of Interpreter is a Composite (therefore Iterator and Visitor are also
applicable).
• Iterator can traverse a Composite. Visitor can apply an operation over a Composite.
• Polymorphic Iterators rely on Factory Methods to instantiate the appropriate Iterator
subclass.
• Memento is often used in conjunction with Iterator. An Iterator can use a Memento to
capture the state of an iteration. The Iterator stores the Memento internally.
Code examples
JavaIterator in Java: Before and
after Iterator in Java
C++Iterator in C++ Iterator in C++: Using operators instead of
methods
PHPIterator in PHP
DelphiIterator in Delphi
PythonIterator in Python
READ NEXTRETURN
Mediator Design Pattern  Interpreter
Support our free website and own the eBook!
• 22 design patterns and 8 principles explained in depth
• 406 well-structured, easy to read, jargon-free pages
• 228 clear and helpful illustrations and diagrams
• An archive with code examples in 4 languages
• All devices supported: EPUB/MOBI/PDF formats
 Learn more...

Design Patterns AntiPatterns Refactoring My account Forum Contact us About us
UML
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
