open System
open System.Net.Http
open Newtonsoft.Json
open System.Collections.Generic
open System.IO

module AppConfig = 
    type Config = {
        apikey: string
    }

    let mutable private mainConfig : (Config option) = None 

    let private initializeConfig =
         let textJson = File.ReadAllText("app.config.json")
         mainConfig <- Some(JsonConvert.DeserializeObject<Config>(textJson))
         mainConfig
    
    let getMainConfig = 
        mainConfig


module SimpleHttpClient = 
    let mutable baseAddress = ""
    let httpGet (address :string) =
        try
            let client = new HttpClient()
            client.BaseAddress <- Uri(baseAddress)
            let result = client.GetAsync(address).Result
            Some(result.Content.ReadAsStringAsync().Result)
        with 
        | :? System.Exception -> None 
    
    let constructQuery address query  = 
        let mutable uri = address
        query |> Map.iter((fun key value -> uri <- String.Concat(uri,key,"=",value,"&")))
        uri
        
module WeatherInformation =
    let config = AppConfig.getMainConfig
    let apiAddress = "https://api.openweathermap.org/data/2.5/weather?"
    let apiKey= if config.IsSome then config.Value.apikey else String.Empty

    type Weather = { 
        id: string
        main: string
        description: string
        icon : string
    }

    type Main = {
        main: string
        temp: decimal
        pressure: decimal
        humidity: decimal
        temp_min: decimal
        temp_max: decimal
    }

    type Clouds = {
        all:int
    }

    type WeatherResult = {
        name: string
        clouds: Clouds
        main: Main
        weather: Weather[]
    }

    let private infoForCityJson city = 
        let paramters = ["q",city;"apiKey",apiKey;"units","metric"] |> Map.ofList
        SimpleHttpClient.baseAddress <- apiAddress
        let uri =  SimpleHttpClient.constructQuery apiAddress paramters 
        SimpleHttpClient.httpGet uri
    
    let private mainWeatherDescription (weather:WeatherResult) =
        String.Concat("Weather for ",
                       weather.name,": ",weather.weather.[0].main,", ",
                       weather.weather.[0].description,".")
    let private weatherTemperature (weather:WeatherResult) =
         String.Concat("Tempareture in Celsius: ",weather.main.temp,
                      ", Max: ",weather.main.temp_max, 
                      ", Min: ",weather.main.temp_min,".") 
                                                            
    let private weatherClouds (weather: WeatherResult) = 
        String.Concat("Clouds in percetage: ",weather.clouds.all)
    
    let private isValidWeather (weather: WeatherResult) = 
        not (isNull(weather.weather) || weather.weather.Length < 1)
    
    let private deserializeWeahterJson weather = 
        let weatherResult = JsonConvert.DeserializeObject<WeatherResult>(weather)
        if isValidWeather weatherResult then Some(weatherResult) else None

    let describeWeather (weather:WeatherResult) =
        let results = new List<string>()
        results.Add(mainWeatherDescription weather)
        results.Add(weatherTemperature weather)
        results.Add(weatherClouds weather)
        results

    let infoForCity city =
        let weatherResultRaw = infoForCityJson city
        if weatherResultRaw.IsSome then
            let weatherResult = deserializeWeahterJson weatherResultRaw.Value
            if weatherResult.IsSome then Some(describeWeather weatherResult.Value) else None
        else 
            None       
            
[<EntryPoint>]
let main argv =
    Console.WriteLine "Type city and country code for example (Krakow,pl)"
    let cityName = Console.ReadLine()
    Console.WriteLine "Getting information..."
    let results = (WeatherInformation.infoForCity cityName)
    if results.IsNone || results.Value.Count < 1 then
        printfn "There are no info about weather for provided city. Try again."
    else
        results.Value.ForEach((fun result -> printfn "%A" result))
    0