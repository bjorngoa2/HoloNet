# Refactoring techniques

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring
Refactoring techniques
Composing methods
Much of refactoring is
devoted to correctly
composing methods. In most
cases, excessively long
methods are the root of all
evil. The vagaries of code
inside these methods
conceal the execution logic
and make the method
extremely hard to
understand – and even
harder to change.
The refactoring techniques
in this group streamline
methods, remove code
duplication, and pave the
way for future
improvements.
• Extract Method
• Inline Method
• Extract Variable
• Inline T emp
• Replace T emp
with Query
• Split T emporary
Variable
• Remove
Assignments to
Parameters
• Replace Method
with Method
Object
• Substitute
Algorithm
Moving Features
between Objects
Even if you have distributed
functionality among
different classes in a less-
than-perfect way, there is
still hope.
• Move Method
• Move Field
• Extract Class
• Inline Class
• Hide Delegate
• Remove Middle
Man
• Introduce
Foreign Method
• Introduce Local
Extension

 

These refactoring techniques
show how to safely move
functionality between
classes, create new classes,
and hide implementation
details from public access.
Organizing Data
These refactoring techniques
help with data handling,
replacing primitives with
rich class functionality.
Another important result is
untangling of class
associations, which makes
classes more portable and
reusable.
• Self Encapsulate
Field
• Replace Data
Value with Object
• Change Value to
Reference
• Change
Reference to
Value
• Replace Array
with Object
• Duplicate
Observed Data
• Change
Unidirectional
Association to
Bidirectional
• Change
Bidirectional
Association to
Unidirectional
• Replace Magic
Number with
Symbolic
Constant
• Encapsulate Field
• Encapsulate
Collection
• Replace T ype
Code with Class
• Replace T ype
Code with
Subclasses
• Replace T ype
Code with State/
Strategy
• Replace Subclass
with Fields
Simplifying
Conditional
Expressions
Conditionals tend to get
more and more complicated
in their logic over time, and
• Decompose
Conditional
• Consolidate
Conditional
Expression
• Consolidate
Duplicate
• Replace Nested
Conditional with
Guard Clauses
• Replace
Conditional with
Polymorphism
• Introduce Null

there are yet more
techniques to combat this as
well.
Conditional
Fragments
• Remove Control
Flag
Object
• Introduce
Assertion
Simplifying Method
Calls
These techniques make
method calls simpler and
easier to understand. This, in
turn, simpli�es the interfaces
for interaction between
classes.
• Rename Method
• Add Parameter
• Remove
Parameter
• Separate Query
from Modi�er
• Parameterize
Method
• Replace
Parameter with
Explicit Methods
• Preserve Whole
Object
• Replace
Parameter with
Method Call
• Introduce
Parameter Object
• Remove Setting
Method
• Hide Method
• Replace
Constructor with
Factory Method
• Replace Error
Code with
Exception
• Replace
Exception with
T est
Dealing with
Generalisation
Abstraction has its own
group of refactoring
techniques, primarily
associated with moving
functionality along the class
inheritance hierarchy,
creating new classes and
interfaces, and replacing
inheritance with delegation
and vice versa.
• Pull Up Field
• Pull Up Method
• Pull Up
Constructor Body
• Push Down
Method
• Push Down Field
• Extract Subclass
• Extract
Superclass
• Extract Interface
• Collapse
Hierarchy
• Form T emplate
Method
• Replace
Inheritance with
Delegation
• Replace
Delegation with
Inheritance

READ NEXTRETURN
Composing Methods  Incomplete Library Class
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
T erms / Privacy policy
Reading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more
fun.
 Learn more
