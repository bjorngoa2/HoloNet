# Self Encapsulate Field

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Organizing Data
Self Encapsulate Field
Self-encapsulation is distinct from ordinary Encapsulate Field: the refactoring technique
given here is performed on a private field.
Problem
You use direct access to private fields inside a class.
Solution
Create a getter and setter for the field, and use only them for accessing the field.
class Range {
  private int low, high;
  boolean includes(int arg) {
    return arg >= low && arg <= high;
  }
}
class Range {
  private int low, high;
  boolean includes(int arg) {
    return arg >= getLow() && arg <= getHigh();
  }
  int getLow() {
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Self Encapsulate Field
https://sourcemaking.com/refactoring/self-encapsulate-field 1/3

Why Refactor
Sometimes directly accessing a private field inside a class just is not flexible enough. You want to
be able to initiate a field value when the first query is made or perform certain operations on new
values of the field when they are assigned, or maybe do all this in various ways in subclasses.
Benefits
Indirect access to fields is when a field is acted on via access methods (getters and setters). This
approach is much more flexible than direct access to fields.
First, you can perform complex operations when data in the field is set or received. Lazy
initialization and validation of field values are easily implemented inside field getters and
setters.
Second and more crucially, you can redefine getters and setters in subclasses.
You have the option of not implementing a setter for a field at all. The field value will be
specified only in the constructor, thus making the field unchangeable throughout the entire
object lifespan.
Drawbacks
When direct access to fields is used, code looks simpler and more presentable, although flexibility
is diminished.
How to Refactor
    return low;
  }
  int getHigh() {
    return high;
  }
}
23.08.2026, 11:11 Self Encapsulate Field
https://sourcemaking.com/refactoring/self-encapsulate-field 2/3

1. Create a getter (and optional setter) for the field. They should be either protected (protected) or
public (public).
2. Find all direct invocations of the field and replace them with getter and setter calls.
RETURN READ NEXT
Replace Data Value with Object    Organizing Data
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
23.08.2026, 11:11 Self Encapsulate Field
https://sourcemaking.com/refactoring/self-encapsulate-field 3/3
