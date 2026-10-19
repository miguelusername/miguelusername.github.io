The testing libraries we will be utilizing are as follows:
1. [MSW](https://mswjs.io/)
    - MSW (Mock Service Worker) is how we will be testing the communication between the frontend and the backend, and how we will ensure that the frontend responds to changes in the backend, and updates accordingly
2. Node.JS Testing System
    - The in-built Node.JS testing system is very abstract and can be applicable to many, many applications through how versatile it is.
3. [Supertest](https://github.com/forwardemail/supertest)
    - Combined with the in-built Node.JS testing system, supertest allows for testing endpoints, and is more suited for testing Express.JS than just the base testing system.
4. [Mocha](https://mochajs.org/)
    - Mocha will allow for persisten connections to allow for testing things like cookies and authentication.