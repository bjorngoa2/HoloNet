# Substitute Algorithm

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Composing Methods
Substitute Algorithm
Problem
So you want to replace an existing algorithm with a new one?
Solution
Replace the body of the method that implements the algorithm with a new algorithm.
String foundPerson(String[] people){
  for (int i = 0; i < people.length; i++) {
    if (people[i].equals("Don")){
      return "Don";
    }
    if (people[i].equals("John")){
      return "John";
    }
    if (people[i].equals("Kent")){
      return "Kent";
    }
  }
  return "";
}
String foundPerson(String[] people){
  List candidates =
    Arrays.asList(new String[] {"Don", "John", "Kent"});
  for (int i=0; i < people.length; i++) {
    if (candidates.contains(people[i])) {
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Substitute Algorithm
https://sourcemaking.com/refactoring/substitute-algorithm 1/3

Why Refactor
1. Gradual refactoring is not the only method for improving a program. Sometimes a method is so
cluttered with issues that it is easier to tear down the method and start fresh. And perhaps you
have found an algorithm that is much simpler and more efficient. If this is the case, you should
simply replace the old algorithm with the new one.
2. As time goes on, your algorithm may be incorporated into a well-known library or framework
and you want to get rid of your independent implementation, in order to simplify maintenance.
3. The requirements for your program may change so heavily that your existing algorithm cannot
be salvaged for the task.
How to Refactor
1. Make sure that you have simplified the existing algorithm as much as possible. Move
unimportant code to other methods using Extract Method. The fewer moving parts in your
algorithm, the easier it is to replace.
2. Create your new algorithm in a new method. Replace the old algorithm with the new one and
start testing the program.
3. If the results do not match, return to the old implementation and compare the results. Identify
the causes of the discrepancy. While the cause is often an error in the old algorithm, it is more
likely due to something not working in the new one.
4. When all tests are successfully completed, delete the old algorithm for good!
      return people[i];
    }
  }
  return "";
}
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning course on refactoring. It
has more content and much more fun.
23.08.2026, 11:10 Substitute Algorithm
https://sourcemaking.com/refactoring/substitute-algorithm 2/3

RETURN READ NEXT
Moving Features between Objects    Replace Method with Method Object
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
 Learn more
23.08.2026, 11:10 Substitute Algorithm
https://sourcemaking.com/refactoring/substitute-algorithm 3/3
