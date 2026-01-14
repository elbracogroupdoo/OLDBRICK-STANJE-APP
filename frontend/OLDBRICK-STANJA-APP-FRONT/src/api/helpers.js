import httpClient from "./httpClient";

export async function getReportStatesById(idNaloga){
    const {data} = await httpClient.get(`api/dailyreports/${idNaloga}/state`);
    return data;
}

export async function createNalogByDate(datum){
    const {data} = await httpClient.post("api/dailyreports/for-date", {datum});
    return data;
}

export async function putMeasuredProsuto(idNaloga, prosutoKanta){
    const {data} = (await httpClient.put(`api/dailyreports/${idNaloga}/prosuto-kanta`, {prosutoKanta})).data;
    return data;
}

export async function calculateProsutoRazlika(idNaloga){
    const {data} = await httpClient.post(`api/dailyreports/${idNaloga}/calculate-prosuto-razlika`);
    return data;
}

export async function getNalogByDate(datum) {
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

export async function postDailyReportStates(idNaloga, states){
    const {data} = await httpClient.post(`api/dailyreports/${idNaloga}/states`, states);
    return data;
}

export async function getAllArticles(){
    const {data} = await httpClient.get("api/Beers/allArticles")
    return data;
}

export async function calculateProsutoOnly(idNaloga){
    const {data} = await httpClient.post(`api/dailyreports/${idNaloga}/calculate-prosuto`);
    return data;
}

export async function getAllReportDates(){
    const {data} = await httpClient.get("api/dailyreports/dates");
    return data;
}
export async function getDailyReportJustByDate(date){
    const {data} = await httpClient.get(`/api/dailyreports/by-date?date=${date}`);
    return data; // data is just idNaloga & datum! 
}
// [HttpPost("{idNaloga}/calculate-prosuto-for-each-beer")]

export async function postCalculatedProsutoForEachBeer(idNaloga){
    const {data} = await httpClient.post(`/api/dailyreports/${idNaloga}/calculate-prosuto-for-each-beer`);
    return data;
}
//[HttpGet("total-by-range")]

export async function getByRangeTotalProsuto(from, to) {
  const { data } = await httpClient.get("/api/dailyreports/total-by-range", {
    params: { from, to }, 
  });
  return data;
}
//[HttpGet("range-report-for-oneBeer")]

export async function getProsutoByRangeForEachBeer(from, to){
    const {data} = await httpClient.get("/api/dailyreports/range-report-for-oneBeer", {
        params: {from, to},
    });
    return data;
}

export async function addBeerQuantity(idNaloga, idPiva, kolicina){
    if(!idNaloga || !idPiva) throw new Error("Nedostaje idNaloga ili idPiva");
    if(typeof kolicina !== "number" || kolicina <= 0)
        throw new Error("Kolicina mora biti pozitivan broj");

    const {data} = await httpClient.patch(`/api/dailyreports/${idNaloga}/states/${idPiva}/add`, {kolicina});
    return data;
}

export async function addMoreBeerQuantity(idNaloga, items){
    const {data} = await httpClient.patch(`/api/dailyreports/${idNaloga}/states/add-batch`, items);
    return data;
}

export async function deleteDailyReport(idNaloga){
    if(!idNaloga) throw new error("Ovaj nalog ne postoji");

    await httpClient.delete(`/api/dailyreports/${idNaloga}`)
}

export async function updateDailyReportStatusAndCalculate(idNaloga, items){
    if(!idNaloga) throw new Error("Ovaj nalog ne postoji.");
    if(!Array.isArray(items) || items.length === 0){
        throw new Error("Lista je prazna");
    }
    const {data} = await httpClient.patch(`/api/dailyreports/${idNaloga}/states`, items);
    return data;
}

export async function updateProsutoKantaAndRecalculate(idNaloga, prosutoKanta){
     if (!idNaloga) throw new Error("Ovaj nalog ne postoji.");

  const value = Number(prosutoKanta);
  if (!Number.isFinite(value) || value < 0) {
    throw new Error("Prosuto kanta mora biti broj >= 0.");
  }
  const { data } = await httpClient.put(
    `/api/dailyreports/${idNaloga}/prosuto-kanta/recalculate`,
    { prosutoKanta: value }
  );
  return data;
}
// POTROSNJA VAGE I POS-A ZA SVAKI ID NALOGA
export async function getTotalPotrosnjaVagaAndPos(idNaloga){
 if(!idNaloga) throw new Error("idNaloga is required");

 const {data} = await httpClient.get(`/api/dailyreports/${idNaloga}/total-potrosnja-VAGA-POS`);

 return data;
}

export async function getDayBeforeStates(idNaloga){
    if (!idNaloga) throw new Error("idNaloga is required");

    const {data} = await httpClient.get(`/api/dailyreports/${idNaloga}/day-before-states`)

    return data;
}

export async function getDailyReportByDateWithBiggerOutput(datum){
    const {data} = await httpClient.get("/api/dailyreports/use-date", {params: {
        datum
    },});
    return data;
}

export async function createInventoryReset(dto){
    const { data } = await httpClient.post(
    "/api/dailyreports/inventory-reset",
    dto
  );
  return data;
}

export async function getTotalsSinceLastInventoryReset(idNaloga){
    
    if (!idNaloga) throw new Error("idNaloga je obavezan.");

    const res =  await httpClient.get(`/api/dailyreports/${idNaloga}/totals-since-last-inventory`);
    return res.data; // dto
}

export async function getKesaItemsForDate(datum){
    const {data} = await httpClient.get("/api/dailyreports/inventory-reset/kesa-items",
        {
            params: {datum}
        }
    );
    return data;
}



