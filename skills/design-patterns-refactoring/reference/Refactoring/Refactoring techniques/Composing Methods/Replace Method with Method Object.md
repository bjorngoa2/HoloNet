# Replace Method with Method Object

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Composing Methods
Replace Method with MethodObject
Problem
You have a long method in which the local variables are so intertwined that you cannot apply
Extract Method.
Solution
Transform the method into a separate class so that the local variables become fields of the class.
Then you can split the method into several methods within the same class.
class Order {
  // ...
  public double price() {
    double primaryBasePrice;
    double secondaryBasePrice;
    double tertiaryBasePrice;
    // Perform long computation.
  }
}
class Order {
  // ...
  public double price() {
    return new PriceCalculator(this).compute();
  }
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Replace Method with Method Object
https://sourcemaking.com/refactoring/replace-method-with-method-object 1/3

Why Refactor
A method is too long and you cannot separate it due to tangled masses of local variables that are
hard to isolate from each other.
The first step is to isolate the entire method into a separate class and turn its local variables into
fields of the class.
Firstly, this allows isolating the problem at the class level. Secondly, it paves the way for splitting a
large and unwieldy method into smaller ones that would not fit with the purpose of the original
class anyway.
Benefits
Isolating a long method in its own class allows stopping a method from ballooning in size. This
also allows splitting it into submethods within the class, without polluting the original class with
utility methods.
Drawbacks
Another class is added, increasing the overall complexity of the program.
}
class PriceCalculator {
  private double primaryBasePrice;
  private double secondaryBasePrice;
  private double tertiaryBasePrice;

  public PriceCalculator(Order order) {
    // Copy relevant information from the
    // order object.
  }

  public double compute() {
    // Perform long computation.
  }
}
23.08.2026, 11:10 Replace Method with Method Object
https://sourcemaking.com/refactoring/replace-method-with-method-object 2/3

How to Refactor
1. Create a new class. Name it based on the purpose of the method that you are refactoring.
2. In the new class, create a private field for storing a reference to an instance of the class in which
the method was previously located. It could be used to get some required data from the original
class if needed.
3. Create a separate private field for each local variable of the method.
4. Create a constructor that accepts as parameters the values of all local variables of the method
and also initializes the corresponding private fields.
5. Declare the main method and copy the code of the original method to it, replacing the local
variables with private fields.
6. Replace the body of the original method in the original class by creating a method object and
calling its main method.
RETURN READ NEXT
Substitute Algorithm    Remove Assignments to Parameters
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
23.08.2026, 11:10 Replace Method with Method Object
https://sourcemaking.com/refactoring/replace-method-with-method-object 3/3
