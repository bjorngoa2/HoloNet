# Replace Parameter with Explicit Methods

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Replace Parameter with ExplicitMethods
Problem
A method is split into parts, each of which is run depending on the value of a parameter.
Solution
Extract the individual parts of the method into their own methods and call them instead of the
original method.
void setValue(String name, int value) {
  if (name.equals("height")) {
    height = value;
    return;
  }
  if (name.equals("width")) {
    width = value;
    return;
  }
  Assert.shouldNeverReachHere();
}
void setHeight(int arg) {
  height = arg;
}
void setWidth(int arg) {
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:12 Replace Parameter with Explicit Methods
https://sourcemaking.com/refactoring/replace-parameter-with-explicit-methods 1/3

Why Refactor
A method containing parameter-dependent variants has grown massive. Non-trivial code is run in
each branch and new variants are added very rarely.
Benefits
Improves code readability. It is much easier to understand the purpose of startEngine() than
setValue("engineEnabled", true).
When Not to Use
Do not replace a parameter with explicit methods if a method is rarely changed and new variants
are not added inside it.
How to Refactor
1. For each variant of the method, create a separate method. Run these methods based on the
value of a parameter in the main method.
2. Find all places where the original method is called. In these places, place a call for one of the
new parameter-dependent variants.
3. When no calls to the original method remain, delete it.
  width = arg;
}
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:12 Replace Parameter with Explicit Methods
https://sourcemaking.com/refactoring/replace-parameter-with-explicit-methods 2/3

RETURN READ NEXT
Preserve Whole Object    Parameterize Method
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
23.08.2026, 11:12 Replace Parameter with Explicit Methods
https://sourcemaking.com/refactoring/replace-parameter-with-explicit-methods 3/3
