# Replace Parameter with Method Call

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Replace Parameter with MethodCall
Problem
Before a method call, a second method is run and its result is sent back to the first method as an
argument. But the parameter value could have been obtained inside the method being called.
Solution
Instead of passing the value through a parameter, place the value-getting code inside the method.
Why Refactor
A long list of parameters is hard to understand. In addition, calls to such methods often resemble a
series of cascades, with winding and exhilarating value calculations that are hard to navigate yet
int basePrice = quantity * itemPrice;
double seasonDiscount = this.getSeasonalDiscount();
double fees = this.getFees();
double finalPrice = discountedPrice(basePrice, seasonDiscount, fees);
int basePrice = quantity * itemPrice;
double finalPrice = discountedPrice(basePrice);
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:12 Replace Parameter with Method Call
https://sourcemaking.com/refactoring/replace-parameter-with-method-call 1/3

have to be passed to the method. So if a parameter value can be calculated with the help of a
method, do this inside the method itself and get rid of the parameter.
Benefits
We get rid of unneeded parameters and simplify method calls. Such parameters are often created
not for the project as it is now, but with an eye for future needs that may never come.
Drawbacks
You may need the parameter tomorrow for other needs... making you rewrite the method.
How to Refactor
1. Make sure that the value-getting code does not use parameters from the current method, since
they will be unavailable from inside another method. If so, moving the code is not possible.
2. If the relevant code is more complicated than a single method or function call, use Extract
Method to isolate this code in a new method and make the call simple.
3. In the code of the main method, replace all references to the parameter being replaced with
calls to the method that gets the value.
4. Use Remove Parameter to eliminate the now-unused parameter.
RETURN READ NEXT
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:12 Replace Parameter with Method Call
https://sourcemaking.com/refactoring/replace-parameter-with-method-call 2/3

Introduce Parameter Object    Preserve Whole Object
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:12 Replace Parameter with Method Call
https://sourcemaking.com/refactoring/replace-parameter-with-method-call 3/3
