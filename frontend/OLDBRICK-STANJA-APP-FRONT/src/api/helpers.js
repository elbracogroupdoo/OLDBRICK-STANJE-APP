import httpClient from "./httpClient";

async function getReportStatesById(idNaloga){
    const {data} = await httpClient.get(`api/dailyreports/${idNaloga}/state`);
    return data;
}


export {
    getReportStatesById
};