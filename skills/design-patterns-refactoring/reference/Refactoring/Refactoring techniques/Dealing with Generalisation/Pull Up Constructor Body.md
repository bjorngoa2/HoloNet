# Pull Up Constructor Body

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Dealing with Generalisation
Pull Up Constructor Body
Problem
Your subclasses have constructors with code that is mostly identical.
Solution
Create a superclass constructor and move the code that is the same in the subclasses to it. Call the
superclass constructor in the subclass constructors.
class Manager extends Employee {
  public Manager(String name, String id, int grade) {
    this.name = name;
    this.id = id;
    this.grade = grade;
  }
  // ...
}
class Manager extends Employee {
  public Manager(String name, String id, int grade) {
    super(name, id);
    this.grade = grade;
  }
  // ...
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:12 Pull Up Constructor Body
https://sourcemaking.com/refactoring/pull-up-constructor-body 1/3

Why Refactor
How is this refactoring technique different from Pull Up Method?
1. In Java, subclasses cannot inherit a constructor, so you cannot simply apply Pull Up Method to
the subclass constructor and delete it after removing all the constructor code to the superclass.
In addition to creating a constructor in the superclass it is necessary to have constructors in the
subclasses with simple delegation to the superclass constructor.
2. In C++ and Java (if you did not explicitly call the superclass constructor) the superclass
constructor is automatically called prior to the subclass constructor, which makes it necessary to
move the common code only from the beginning of the subclass constructors (since you will not
be able to call the superclass constructor from an arbitrary place in a subclass constructor).
3. In most programming languages, a subclass constructor can have its own list of parameters
different from the parameters of the superclass. Therefore you should create a superclass
constructor only with the parameters that it truly needs.
How to Refactor
1. Create a constructor in a superclass.
2. Extract the common code from the beginning of the constructor of each subclass to the
superclass constructor. Before doing so, try to move as much common code as possible to the
beginning of the constructor.
3. Place the call for the superclass constructor in the first line in the subclass constructors.
RETURN READ NEXT
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:12 Pull Up Constructor Body
https://sourcemaking.com/refactoring/pull-up-constructor-body 2/3

Push Down Method    Pull Up Method
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:12 Pull Up Constructor Body
https://sourcemaking.com/refactoring/pull-up-constructor-body 3/3
