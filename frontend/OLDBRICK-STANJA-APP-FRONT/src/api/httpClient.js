import axios from "axios";
import { getToken, removeToken } from "./tokenStorage";

var httpClient = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL,
});

httpClient.interceptors.request.use(
    function(config){
        var token = getToken();

        if(token){
            config.headers.Authorization = "Bearer" + token;
        }

        return config;
    },
    function(error){
        return Promise.reject(error);
    }
);

httpClient.interceptors.response.use(
    function(response){
        return response;
    },
    function(error){
        if(error && error.response && error.response.status === 401){
            removeToken();
        }
        return Promise.reject(error);
    }
)
export default httpClient;