# Inline Method

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Composing Methods
Inline Method
Problem
When a method body is more obvious than the method itself, use this technique.
Solution
Replace calls to the method with the method’s content and delete the method itself.
class PizzaDelivery {
  // ...
  int getRating() {
    return moreThanFiveLateDeliveries() ? 2 : 1;
  }
  boolean moreThanFiveLateDeliveries() {
    return numberOfLateDeliveries > 5;
  }
}
class PizzaDelivery {
  // ...
  int getRating() {
    return numberOfLateDeliveries > 5 ? 2 : 1;
  }
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Inline Method
https://sourcemaking.com/refactoring/inline-method 1/2

Why Refactor
A method simply delegates to another method. In itself, this delegation is no problem. But when
there are many such methods, they become a confusing tangle that is hard to sort through.
Often methods are not too short originally, but become that way as changes are made to the
program. So don’t be shy about getting rid of methods that have outlived their use.
Benefits
By minimizing the number of unneeded methods, you make the code more straightforward.
How to Refactor
1. Make sure that the method is not redefined in subclasses. If the method is redefined, refrain
from this technique.
2. Find all calls to the method. Replace these calls with the content of the method.
3. Delete the method.
RETURN READ NEXT
Extract Variable    Extract Method
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
23.08.2026, 11:10 Inline Method
https://sourcemaking.com/refactoring/inline-method 2/2
