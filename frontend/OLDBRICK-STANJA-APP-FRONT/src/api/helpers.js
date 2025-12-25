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
async function getDailyReportJustByDate(date){
    const {data} = await httpClient.get(`/api/dailyreports/by-date?date=${date}`);
    return data; // data is just idNaloga & datum! 
}
// [HttpPost("{idNaloga}/calculate-prosuto-for-each-beer")]

async function postCalculatedProsutoForEachBeer(idNaloga){
    const {data} = await httpClient.post(`/api/dailyreports/${idNaloga}/calculate-prosuto-for-each-beer`);
    return data;
}
//[HttpGet("total-by-range")]

async function getByRangeTotalProsuto(from, to) {
  const { data } = await httpClient.get("/api/dailyreports/total-by-range", {
    params: { from, to }, 
  });
  return data;
}
//[HttpGet("range-report-for-oneBeer")]

async function getProsutoByRangeForEachBeer(from, to){
    const {data} = await httpClient.get("/api/dailyreports/range-report-for-oneBeer", {
        params: {from, to},
    });
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
    getAllReportDates,
    getDailyReportJustByDate,
    postCalculatedProsutoForEachBeer,
    getByRangeTotalProsuto,
    getProsutoByRangeForEachBeer
};