# Encapsulate Field

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Organizing Data
Encapsulate Field
Problem
You have a public field.
Solution
Make the field private and create access methods for it.
Why Refactor
class Person {
  public String name;
}
class Person {
  private String name;
  public String getName() {
    return name;
  }
  public void setName(String arg) {
    name = arg;
  }
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Encapsulate Field
https://sourcemaking.com/refactoring/encapsulate-field 1/3

One of the pillars of object-oriented programming is Encapsulation, the ability to conceal object
data. Otherwise, all objects would be public and other objects could get and modify the data of
your object without any checks and balances! Data is separated from the behaviors associated with
this data, modularity of program sections is compromised, and maintenance becomes complicated.
Benefits
If the data and behavior of a component are closely interrelated and are in the same place in
the code, it is much easier for you to maintain and develop this component.
You can also perform complicated operations related to access to object fields.
When Not to Use
In some cases, encapsulation is ill-advised due to performance considerations. These cases are rare
but when they happen, this circumstance is very important.
Say that you have a graphical editor that contains objects possessing x- and y-coordinates. These
fields are unlikely to change in the future. What’s more, the program involves a great many
different objects in which these fields are present. So accessing the coordinate fields directly saves
significant CPU cycles that would otherwise be taken up by calling access methods.
As an example of this unusual case, there is the Point class in Java. All fields of this class are public.
How to Refactor
1. Create a getter and setter for the field.
2. Find all invocations of the field. Replace receipt of the field value with the getter, and replace
setting of new field values with the setter.
3. After all field invocations have been replaced, make the field private.
Next Steps
23.08.2026, 11:11 Encapsulate Field
https://sourcemaking.com/refactoring/encapsulate-field 2/3

Encapsulate Field is only the first step in bringing data and the behaviors involving this data closer
together. After you create simple methods for access fields, you should recheck the places where
these methods are called. It is quite possible that the code in these areas would look more
appropriate in the access methods.
RETURN READ NEXT
Encapsulate Collection    Replace Magic Number with Symbolic
Constant
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
23.08.2026, 11:11 Encapsulate Field
https://sourcemaking.com/refactoring/encapsulate-field 3/3
