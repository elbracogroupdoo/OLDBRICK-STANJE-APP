import httpClient from "./httpClient";

async function getReportStatesById(idNaloga){
    const {data} = await httpClient.get(`api/dailyreports/${idNaloga}/state`);
    return data;
}

async function createNalogByDate(datum){
    const {data} = await httpClient.post("api/dailyreports/for-date", {datum});
    return data;
}

async function putMeasuredProsuto(idNaloga, prosutoKanta){
    const {data} = (await httpClient.put(`api/dailyreports/${idNaloga}/prosuto-kanta`, {prosutoKanta})).data;
    return data;
}

async function calculateProsutoRazlika(idNaloga){
    const {data} = await httpClient.post(`api/dailyreports/${idNaloga}/calculate-prosuto-razlika`);
    return data;
}

async function getNalogByDate(datum) {
  try{
    const { data } = await httpClient.get("/api/dailyreports/use-date", {
    params: { datum },
  });
  return data;
  }catch(error){
    if(error?.response?.status === 404) return null;
    throw error;
  }
}

async function postDailyReportStates(idNaloga, states){
    const {data} = await httpClient.post(`api/dailyreports/${idNaloga}/states`, states);
    return data;
}

async function getAllArticles(){
    const {data} = await httpClient.get("api/Beers/allArticles")
    return data;
}

async function calculateProsutoOnly(idNaloga){
    const {data} = await httpClient.post(`api/dailyreports/${idNaloga}/calculate-prosuto`);
    return data;
}

async function getAllReportDates(){
    const {data} = await httpClient.get("api/dailyreports/dates");
    return data;
}


export {
    getReportStatesById,
    createNalogByDate,
    putMeasuredProsuto,
    calculateProsutoRazlika,
    getNalogByDate,
    postDailyReportStates,
    getAllArticles,
    calculateProsutoOnly,
    getAllReportDates
};