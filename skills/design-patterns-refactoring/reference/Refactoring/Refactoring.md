# Refactoring

Source: sourcemaking.com (downloaded copy)

---

Refactoring
Bad code smells
Bloaters
Bloaters are code, methods
and classes that have
increased to such
gargantuan proportions that
they are hard to work with.
Usually these smells do not
crop up right away, rather
they accumulate over time
as the program evolves (and
especially when nobody
makes an effort to eradicate
them).
• Long Method
• Large Class
• Primitive
Obsession
• Long Parameter
List
• Data Clumps
Object-Orientation
Abusers
All these smells are
incomplete or incorrect
application of object-
oriented programming
principles.
• Switch
Statements
• T emporary Field
• Refused Bequest
• Alternative
Classes with
Different
Interfaces

 

Refactoring techniques
Change Preventers
These smells mean that if
you need to change
something in one place in
your code, you have to make
many changes in other
places too. Program
development becomes much
more complicated and
expensive as a result.
• Divergent
Change
• Shotgun Surgery
• Parallel
Inheritance
Hierarchies
Dispensables
A dispensable is something
pointless and unneeded
whose absence would make
the code cleaner, more
ef�cient and easier to
understand.
• Comments
• Duplicate Code
• Lazy Class
• Data Class
• Dead Code
• Speculative
Generality
Couplers
All the smells in this group
contribute to excessive
coupling between classes or
show what happens if
coupling is replaced by
excessive delegation.
• Feature Envy
• Inappropriate
Intimacy
• Message Chains
• Middle Man
• Incomplete
Library Class

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
These refactoring techniques
show how to safely move
functionality between
classes, create new classes,
and hide implementation
details from public access.
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
there are yet more
techniques to combat this as
well.
• Decompose
Conditional
• Consolidate
Conditional
Expression
• Consolidate
Duplicate
Conditional
Fragments
• Remove Control
Flag
• Replace Nested
Conditional with
Guard Clauses
• Replace
Conditional with
Polymorphism
• Introduce Null
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
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About usReading is boring
Aren't you bored of reading so much? Try out our new interactive
learning course on refactoring. It has more content and much more

READ NEXT
Code Smells 
© 2007-2026 SourceMaking.com T erms / Privacy policy
All rights reserved.
fun.
 Learn more
