using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Fathym;
using LCU.Presentation.State.ReqRes;
using LCU.StateAPI.Utilities;
using LCU.StateAPI;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Collections.Generic;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.DevOps;
using LCU.Personas.Enterprises;
using LCU.Personas.Client.Applications;
using LCU.Personas.Client.Identity;
using Fathym.API;
using LCU.Personas.Client.Security;
using AmblOn.State.API.Users.Models;
using LCU.Presentation;
using AmblOn.State.API.Users.Graphs;
using Microsoft.AspNetCore.WebUtilities;
using System.Device.Location;
using Newtonsoft.Json.Linq;
using static AmblOn.State.API.Users.Host.Startup;

namespace AmblOn.State.API.Itineraries.State
{
    public class ItinerariesStateHarness : LCUStateHarness<ItinerariesState>
    {
        #region Constants
        #endregion

        #region Fields 
        #endregion

        #region Properties 
        #endregion

        #region Constructors
        public ItinerariesStateHarness(ItinerariesState state)
            : base(state ?? new ItinerariesState())
        { }
        #endregion

        #region API Methods

        #region Add

        public virtual async Task AddItinerary(AmblOnGraph amblGraph, string username, string entLookup, Itinerary itinerary)
        {
            ensureStateObject();

            itinerary.CreatedDateTime = DateTime.Now;
            itinerary.Shared = false;
            itinerary.Editable = true;

            var itineraryResp = await amblGraph.AddItinerary(username, entLookup, itinerary);

            if (itineraryResp.Status)
            {
                itinerary.ID = itineraryResp.Model;

                await itinerary.ActivityGroups.Each(async (activityGroup) =>
                {
                    activityGroup.CreatedDateTime = DateTime.Now;

                    var activityGroupResp = await amblGraph.AddActivityGroup(username, entLookup, itinerary.ID, activityGroup);

                    if (activityGroupResp.Status)
                    {
                        activityGroup.ID = activityGroupResp.Model;

                        await activityGroup.Activities.Each(async (activity) =>
                        {
                            activity.CreatedDateTime = DateTime.Now;

                            var activityResp = await amblGraph.AddActivityToAG(username, entLookup, itinerary.ID, activityGroup.ID, activity);

                            if (activityResp.Status)
                            {
                                activity.ID = activityResp.Model;
                            }
                        });
                    }
                });

                if (!State.UserItineraries.Any(x => x.ID == itinerary.ID))
                    State.UserItineraries.Add(itinerary);
            }

            State.Loading = false;
        }

        #region Delete

        public virtual async Task DeleteItineraries(AmblOnGraph amblGraph, string username, string entLookup, List<Guid> itineraryIDs)
        {
            ensureStateObject();

            var success = true;

            await itineraryIDs.Each(async (itineraryID) =>
            {
                var itinerary = State.UserItineraries.FirstOrDefault(x => x.ID == itineraryID);

                if (itinerary != null)
                {
                    await itinerary.ActivityGroups.Each(async (activityGroup) =>
                    {
                        await activityGroup.Activities.Each(async (activity) =>
                        {
                            var actResp = await amblGraph.DeleteActivity(username, entLookup, itinerary.ID, activityGroup.ID, activity.ID);

                            if (!actResp.Status)
                                success = false;
                        });

                        if (success)
                        {
                            var actGroupResp = await amblGraph.DeleteActivityGroup(username, entLookup, itinerary.ID, activityGroup.ID);

                            if (!actGroupResp.Status)
                                success = false;
                        }
                    });

                    if (success)
                    {
                        var itineraryResp = await amblGraph.DeleteItinerary(username, entLookup, itineraryID);

                        if (!itineraryResp.Status)
                            success = false;
                    }

                    if (success)
                    {
                        var existing = State.UserItineraries.FirstOrDefault(x => x.ID == itineraryID);

                        if (existing != null)
                            State.UserItineraries.Remove(existing);

                        State.UserItineraries = State.UserItineraries.Distinct().ToList();
                    }
                }
                else
                    success = false;
            });

            if (!success)
                State.Error = "General Error";

            State.Loading = false;
        }

        #endregion

        #region Edit
        
        public virtual async Task EditItinerary(AmblOnGraph amblGraph, string username, string entLookup, Itinerary itinerary, List<ActivityLocationLookup> activityLocations)
        {
            State.Loading = true;
            
            ensureStateObject();

            var activitiesList = new List<Activity>();

            if(!activityLocations.IsNullOrEmpty()){       
                activitiesList =  await addLocationFromActivity(amblGraph, username, entLookup, activityLocations);           
            }

            var existing = State.UserItineraries.FirstOrDefault(x => x.ID == itinerary.ID);

            if (existing != null)
            {
                if (existing.Editable)
                {
                    var success = true;

                    var itineraryResp = await amblGraph.EditItinerary(username, entLookup, itinerary);

                    if (!itineraryResp.Status)
                        success = false;

                    if (success)
                    {
                        await itinerary.ActivityGroups.Each(async (activityGroup) =>
                        {
                            var agExisting = existing.ActivityGroups.FirstOrDefault(x => x.ID == activityGroup.ID);

                            if (agExisting == null)
                            {
                                var addActGResp = await amblGraph.AddActivityGroup(username, entLookup, itinerary.ID, activityGroup);

                                if (addActGResp.Status)
                                {
                                    activityGroup.ID = addActGResp.Model;

                                    await activityGroup.Activities.Each(async (activity) =>
                                    {
                                        var addActResp = new BaseResponse<Guid>();
                                        
                                        if(activity.ID == null){
                                            addActResp = await amblGraph.AddActivityToAG(username, entLookup, itinerary.ID, activityGroup.ID, activity);

                                            activity.ID = addActResp.Model;

                                            var exists = activitiesList?.FirstOrDefault(x => x.Title == activity.Title);

                                            if(exists != null){
                                                exists.ID = activity.ID;
                                         
                                                addActResp = await amblGraph.AddActivityToAG(username, entLookup, itinerary.ID, activityGroup.ID, exists);
                                            }
                                        }

                                        else{
                                            var exists = activitiesList?.FirstOrDefault(x => x.ID == activity.ID);

                                            addActResp = await amblGraph.AddActivityToAG(username, entLookup, itinerary.ID, activityGroup.ID, exists);
                                        }
                                        
                                        activity.ID = addActResp.Model;

                                        if (!addActResp.Status)
                                            success = false;
                                    });
                                }
                                else
                                    success = false;
                            }
                            else
                            {
                                await activityGroup.Activities.Each(async (activity) =>
                                {
                                    var aExisting = agExisting.Activities.FirstOrDefault(x => x.ID == activity.ID);

                                    if (aExisting == null)
                                    {
                                        var exists = activitiesList?.FirstOrDefault(x => x.ID == activity.ID);

                                        var addActResp = await amblGraph.AddActivityToAG(username, entLookup, itinerary.ID, activityGroup.ID, exists ?? activity);

                                        activity.ID = addActResp.Model;

                                        if (!addActResp.Status)
                                            success = false;
                                    }
                                    else
                                    {
                                        var exists = activitiesList?.FirstOrDefault(x => x.ID == activity.ID);

                                        var editActResp = await amblGraph.EditActivity(username, entLookup, exists ?? activity);

                                        if (!editActResp.Status)
                                            success = false;
                                    }
                                });

                                var editActGResp = await amblGraph.EditActivityGroup(username, entLookup, activityGroup);

                                if (!editActGResp.Status)
                                    success = false;
                            }
                        });

                        await existing.ActivityGroups.Each(async (activityGroup) =>
                        {
                            var agNew = itinerary.ActivityGroups.FirstOrDefault(x => x.ID == activityGroup.ID);

                            if (agNew == null)
                            {
                                await activityGroup.Activities.Each(async (activity) =>
                                {
                                    var delActResp = await amblGraph.DeleteActivity(username, entLookup, itinerary.ID, activityGroup.ID, activity.ID);

                                    if (!delActResp.Status)
                                        success = false;
                                });

                                if (success)
                                {
                                    var delActGResp = await amblGraph.DeleteActivityGroup(username, entLookup, itinerary.ID, activityGroup.ID);

                                    if (!delActGResp.Status)
                                        success = false;
                                }
                            }
                            else
                            {
                                await activityGroup.Activities.Each(async (activity) =>
                                {
                                    var aNew = agNew.Activities.FirstOrDefault(x => x.ID == activity.ID);

                                    if (aNew == null)
                                    {
                                        var delActResp = await amblGraph.DeleteActivity(username, entLookup, itinerary.ID, activityGroup.ID, activity.ID);

                                        if (!delActResp.Status)
                                            success = false;
                                    }
                                });
                            }
                        });
                    }

                    if (success)
                        State.UserItineraries = await fetchUserItineraries(amblGraph, username, entLookup);
                    else
                        State.Error = "General Error updating user itinerary.";
                }
                else State.Error = "Cannot edit a shared itinerary.";

            }
            else
                State.Error = "Itinerary not found.";

            State.Loading = false;
        }

        public virtual async Task ItineraryItemOrderAdjusted(AmblOnGraph amblGraph, string email, string entLookup, Itinerary itinerary, Guid? activityChanged)
        {
            var baseQuery = "g.V(\"" + itinerary.ID.ToString() + "\").Out(\"Contains\").coalesce(";

            var aGquery = "";

            var aQuery = "";

            itinerary.ActivityGroups.ForEach(
                (activitygroup) => {
                    aGquery = aGquery + "has(\"id\", \"" + activitygroup.ID.ToString() + "\").property(\"Order\", \"" + activitygroup.Order.ToString()  + "\").property(\"Title\", \"" + activitygroup.Title.ToString() + "\"),";

                    activitygroup.Activities.ForEach(
                        (activity) => {
                            if(activity.ID.ToString() == activityChanged.ToString()){
                                Random rnd = new Random();

                                var vertexMoveEdge = rnd.Next(1, 10000);                          

                                aQuery = aQuery + "has(\"id\", \"" + activity.ID.ToString() + "\").as(\"" + vertexMoveEdge.ToString() + "\").property(\"Order\", \"" + activity.Order.ToString() + "\").inE(\"Contains\").sideEffect(drop()).V(\"" + activitygroup.ID.ToString() + "\").addE(\"Contains\").to(\"" + vertexMoveEdge.ToString() + "\"),";
                            }
                            else{
                                aQuery = aQuery + "has(\"id\", \"" + activity.ID.ToString() + "\").property(\"Order\", \"" + activity.Order.ToString() + "\"),";
                            }
                        });
                });

            var query = baseQuery + aGquery + ").out(\"Contains\").coalesce(" + aQuery + ")";

            //var resp = await amblGraph.EditOrder(email, entLookup, query);

            State.UserItineraries = await fetchUserItineraries(amblGraph, email, entLookup);
        } 

        public virtual async Task QuickEditActivity(AmblOnGraph amblGraph, string username, string entLookup, Activity activity)
        {
            var resp = await amblGraph.QuickEditActivity(activity);

            State.UserItineraries = await fetchUserItineraries(amblGraph, username, entLookup);

            State.Loading = false;
        }
        
        #endregion

        public virtual async Task RefreshItineraries(AmblOnGraph amblGraph, string entLookup, string username)
        {
            ensureStateObject();

            var userInfoResp = await amblGraph.GetUserInfo(username, entLookup);

            if (userInfoResp.Status)
            {
                State.UserInfo = userInfoResp.Model;
                State.UserInfo.Email = username;
            }

            State.UserItineraries = await fetchUserItineraries(amblGraph, username, entLookup);

            State.Loading = false;
        }

        public virtual async Task ShareItineraries(ApplicationManagerClient appMgr, AmblOnGraph amblGraph, string username, string entLookup, 
            List<Itinerary> itineraries, List<string> usernames)
        {
            ensureStateObject();

            var name = username;

            if (State.UserInfo != null)
                name = State.UserInfo.FirstName + " " + State.UserInfo.LastName;

            var subject = Environment.GetEnvironmentVariable("SHARED-ITINERARY-EMAIL-SUBJECT").Replace("%%USER-NAME%%", name);
            var message = Environment.GetEnvironmentVariable("SHARED-ITINERARY-EMAIL").Replace("%%BASE-URL%%", Environment.GetEnvironmentVariable("BASE-URL"));
            var from = Environment.GetEnvironmentVariable("FROM-EMAIL");

            Dictionary<string, string> results = new Dictionary<string, string>();

            var success = true;

            await usernames.Each(async (user) =>
            {
                await itineraries.Each(async (itinerary) =>
                {

                    var result = await amblGraph.ShareItinerary(username, entLookup, itinerary, user);

                    await addLocationFromSharedItinerary(amblGraph, user, entLookup, itinerary);

                    State.SharedStatus = result.Status;
                    
                    if (State.SharedStatus){
                        var mail = new
                        {
                            EmailTo = user,
                            EmailFrom = from,
                            Subject = subject,
                            Content = message
                        };

                        var meta = new MetadataModel();

                        meta.Metadata["AccessRequestEmail"] = JToken.FromObject(mail);

                        var resp = await appMgr.SendAccessRequestEmail(meta, entLookup);

                        State.SharedStatus = resp.Status;
                    }
                });
            });

            State.Loading = false;
        }

        public virtual async Task UnshareItineraries(AmblOnGraph amblGraph, string entLookup, List<Itinerary> itineraries, List<string> usernames)
        {
            ensureStateObject();

            var success = true;

            await usernames.Each(async (username) =>
            {
                await itineraries.Each(async (itinerary) =>
                {
                    var result = await amblGraph.UnshareItinerary(username, entLookup, itinerary, username);

                    if (!result.Status)
                        success = false;
                });
            });

            if (!success)
                State.Error = "General Error unsharing itinerary.";

            State.Loading = false;
        }
        #endregion
        #endregion

        #region Helpers

        protected virtual async Task<List<Activity>> addLocationFromActivity(AmblOnGraph amblGraph, string email, string entLookup, List<ActivityLocationLookup> activityLocations)
        {
            var activities = new List<Activity>();

            foreach (ActivityLocationLookup acLoc in activityLocations){
                var location = await amblGraph.ensureLocation(email, entLookup, acLoc.Location);

                acLoc.Activity.LocationID = location.ID;
                
                activities.Add(acLoc.Activity); 

                // var existing = State.AllUserLocations.FirstOrDefault(x => x.ID == location.ID);

                // if (existing == null){
                //     State.AllUserLocations.Add(location);
                // }                   
            }
            return activities;                              
        }

        protected virtual async Task<Status> addLocationFromSharedItinerary(AmblOnGraph amblGraph, string email, string entLookup, Itinerary itinerary)
        {
            await itinerary.ActivityGroups.Each(async (activityGroup) =>
            {
                await activityGroup.Activities.Each(async (activity) =>
                {   
                    if (activity.LocationID.HasValue && activity.LocationID != Guid.Empty){
                        var location = await amblGraph.ensureLocation(email, entLookup, activity.LocationID);

                        // var existing = State.AllUserLocations.FirstOrDefault(x => x.ID == location.ID);

                        // if (existing == null){
                        //     State.AllUserLocations.Add(location);
                        // }     
                    }                      
                });
            });                    
            return Status.Success;                              
        }

        protected virtual void ensureStateObject()
        {
            State.Error = "";

            State.SharedStatus = null;

            //State.Status = "";

            if (State.UserItineraries == null)
                State.UserItineraries = new List<Itinerary>();
        }

        protected virtual async Task<List<Itinerary>> fetchUserItineraries(AmblOnGraph amblGraph, string username, string entLookup)
        {
            var itineraries = await amblGraph.ListItineraries(username, entLookup);

            return itineraries;
        }

        #endregion

    }
}
