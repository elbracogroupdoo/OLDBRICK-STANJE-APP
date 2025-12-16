import httpClient from "./httpClient";
import { setToken } from "./tokenStorage";


async function login(credentials){
    var response = await httpClient.post("/api/auth/login", {
        username: credentials.username,
        password: credentials.password
    });

    var token = response.data.token || (response.data.response && response.data.response.token);

    if (!token) {
    throw new Error("Token is missing in login response");
  }

  setToken(token);
  return response.data;

}

async function registerUser(payload) {
  var response = await httpClient.post("/api/users", payload);
  return response.data;
}

export {
  login,
  registerUser
};