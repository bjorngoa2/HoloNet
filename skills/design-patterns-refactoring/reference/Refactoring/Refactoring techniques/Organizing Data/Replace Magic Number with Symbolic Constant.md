# Replace Magic Number with Symbolic Constant

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Organizing Data
Replace Magic Number withSymbolic Constant
Problem
Your code uses a number that has a certain meaning to it.
Solution
Replace this number with a constant that has a human-readable name explaining the meaning of
the number.
Why Refactor
double potentialEnergy(double mass, double height) {
  return mass * height * 9.81;
}
static final double GRAVITATIONAL_CONSTANT = 9.81;
double potentialEnergy(double mass, double height) {
  return mass * height * GRAVITATIONAL_CONSTANT;
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Replace Magic Number with Symbolic Constant
https://sourcemaking.com/refactoring/replace-magic-number-with-symbolic-constant 1/3

A magic number is a numeric value that is encountered in the source but has no obvious meaning.
This “anti-pattern” makes it harder to understand the program and refactor the code.
Yet more difficulties arise when you need to change this magic number. Find and replace won’t
work for this: the same number may be used for different purposes in different places, meaning
that you will have to verify every line of code that uses this number.
Benefits
The symbolic constant can serve as live documentation of the meaning of its value.
It is much easier to change the value of a constant than to search for this number throughout
the entire codebase, without the risk of accidentally changing the same number used elsewhere
for a different purpose.
Reduce duplicate use of a number or string in the code. This is especially important when the
value is complicated and long (such as 3.14159 or 0xCAFEBABE).
Good to Know
Not all numbers are magical.
If the purpose of a number is obvious, there is no need to replace it. A classic example is:
for (i = 0; i < сount; i++) ... ;
Alternatives
1. Sometimes a magic number can be replaced with method calls. For example, if you have a
magic number that signifies the number of elements in a collection, you do not need to use it
for checking the last element of the collection. Instead, use the standard method for getting the
collection length.
2. Magic numbers are sometimes used as type code. Say that you have two types of users and you
use a number field in a class to specify which is which: administrators are 1 and ordinary users
are 2.
23.08.2026, 11:11 Replace Magic Number with Symbolic Constant
https://sourcemaking.com/refactoring/replace-magic-number-with-symbolic-constant 2/3

In this case, you should use one of the refactoring methods to avoid type code:
Replace Type Code with Class
Replace Type Code with Subclasses
Replace Type Code with State/Strategy
How to Refactor
1. Declare a constant and assign the value of the magic number to it.
2. Find all mentions of the magic number.
3. For each of the numbers that you find, double-check that the magic number in this particular
case corresponds to the purpose of the constant. If yes, replace the number with your constant.
This is an important step, since the same number can mean absolutely different things (and
replaced with different constants, as the case may be).
RETURN READ NEXT
Encapsulate Field    Change Bidirectional Association to
Unidirectional
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
23.08.2026, 11:11 Replace Magic Number with Symbolic Constant
https://sourcemaking.com/refactoring/replace-magic-number-with-symbolic-constant 3/3
